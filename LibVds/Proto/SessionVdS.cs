﻿using LibVds.Proto.Apdu;
using NLog;

namespace LibVds.Proto
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    public class SessionVdS
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Current send counter, must be incremented with each new outgoing frame
        public uint MySendCounter { get; private set; }

        // The last received TC of the remote peer
        public uint OtherSendCounter { get; private set; }

        // indicates whether a key (AES/CHIASMUS) is used or not
        public bool IsSecured { get; set; }

        // AES/CHIASMUS key or 0 in case of unsecured communication
        public ushort KeyNumber { get; set; }

        public byte MyAesLen { get; set; }

        public int OtherAesLen { get; set; }

        private readonly Stream stream;

        private readonly CancellationTokenSource cts;

        public static Dictionary<ushort, byte[]> AesKeyList = new Dictionary<ushort, byte[]>();

        private readonly ConcurrentQueue<FrameVdS> transmitQueue = new ConcurrentQueue<FrameVdS>();

        private readonly bool isServer;

        private static readonly Random rnd = new Random();

        private string Type
        {
            get
            {
                return this.isServer ? "SVR" : "CLT";
            }
        }

        public SessionVdS(Stream stream, bool isServer, ushort keyNumber)
        {
            this.stream = stream;
            this.cts = new CancellationTokenSource();
            this.isServer = isServer;
            this.MyAesLen = 160;
            //this.OtherAesLen = 160;

            if (!this.isServer)
            {
                this.KeyNumber = keyNumber;
            }

            this.MySendCounter = (uint)rnd.Next(1, 100);
            Log.Info("Session initialized with TC " + this.MySendCounter);
            this.Run();
        }

        public bool IsActive { get; private set; }

        public bool NeedsAck { get; set; }

        public bool IsAcked { get; private set; }

        public int TransmitQueueLength
        {
            get { return this.transmitQueue.Count; }
        }

        public DateTime LastPollReqReceived { get; private set; }

        static SessionVdS()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                for (ushort i = 0; i < 12346; i++)
                {
                    aes.GenerateKey();
                    //AesKeyList[i] = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 };
                    //AesKeyList[i] = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
                    //AesKeyList[i] = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x01, 0x02, 0x03, 0x04, 0x01, 0x02, 0x03, 0x04, 0x01, 0x02, 0x03, 0x04 };
                    //AesKeyList[i] = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };
                    AesKeyList[i] = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78 };
                    //AesKeyList[i] = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x31, 0x32, 0x33, 0x34, 0x31, 0x32, 0x33, 0x34, 0x31, 0x32, 0x33, 0x34 };
                }
            }
        }

        public void AddMessage(FrameVdS frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            if (frame is FrameVdS_02)
            {
                this.NeedsAck = true;
            }

            this.IsAcked = false;  
            this.transmitQueue.Enqueue(frame);
        }

        public bool WaitForTransmission(CancellationToken token, TimeSpan timeout)
        {
            var timeoutCts = new CancellationTokenSource(timeout);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.cts.Token, timeoutCts.Token, token);
            while (!linkedCts.IsCancellationRequested)
            {
                if (!this.transmitQueue.Any())
                {
                    return true;
                }

                linkedCts.Token.WaitHandle.WaitOne(100);
            }

            Log.Error("Waiting for transmission failed");
            return false;
        }

        public bool WaitForAcknowledge(CancellationToken token, TimeSpan timeout)
        {
            var timeoutCts = new CancellationTokenSource(timeout);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.cts.Token, timeoutCts.Token, token);
            while (!linkedCts.IsCancellationRequested)
            {
                if (!this.NeedsAck || this.IsAcked)
                {
                    return true;
                }

                linkedCts.Token.WaitHandle.WaitOne(100);
            }

            Log.Error("Waiting for ACK failed");
            return false;
        }

        public Task Run()
        {
            this.IsActive = true;
            var task = Task.Run(
                () =>
                {
                    var rcvBuffer = new byte[256];
                    var bytes = new List<byte>();
                    while (!this.cts.IsCancellationRequested)
                    {
                        try
                        {
                            var bytesRead = this.stream.Read(rcvBuffer, 0, rcvBuffer.Length);
                            if (bytesRead == 0)
                            {
                                this.cts.Token.WaitHandle.WaitOne(500);
                                continue;
                            }
                            if (bytesRead < 0)
                            {
                                break;
                            }

                            for (int i = 0; i < bytesRead; i++)
                            {
                                bytes.Add(rcvBuffer[i]);
                            }

                            var tmp = bytes.ToArray();
                            if (tmp.Length > 4)
                            {
                                var key = BitConverter.ToUInt16(tmp.Take(2).Reverse().ToArray(), 0);
                                var length = BitConverter.ToUInt16(tmp.Skip(2).Take(2).Reverse().ToArray(), 0);
                                if (tmp.Length < length + 4)
                                {
                                    Log.Info("Incomplete frame, continue reading...");
                                    continue;
                                }

                                Array.Resize(ref tmp, length + 4);
                                Log.Trace("RECEIVED: " + BitConverter.ToString(tmp));
                                var frame = new FrameTcp(key, length, tmp);
                                bytes.RemoveRange(0, tmp.Length);
                                this.HandleReceived(frame);
                            }
                        }
                        catch (IOException e)
                        {
                            Log.ErrorException("IO Exception occured", e);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorException("Exception occured", ex);
                            break;
                        }
                    }

                    this.Close();
                });

            return task;
        }

        public bool Terminate(TimeSpan timeout)
        {
            this.SendResponse(FrameVdS_FF.Create());
            this.IsAcked = false;
            return this.WaitForAcknowledge(cts.Token, timeout);
        }

        public void Close()
        {
            Log.Info("Closing session...");

            try
            {
                this.cts.Cancel();
                this.stream.Close();
            }
            catch (Exception exception)
            {
                Log.ErrorException("Error at closing session", exception);
            }
            finally
            {
                this.IsActive = false;
            }
        }

        /// <summary>
        /// Must be called for each outgoing frame
        /// </summary>
        private void IncrementMySendCounter()
        {
            if (this.MySendCounter == uint.MaxValue - 1)
            {
                this.MySendCounter = 0;
                return;
            }

            this.MySendCounter++;
        }

        /// <summary>
        /// Must be called for each incoming frame
        /// </summary>
        private void IncrementOtherSendCounter()
        {
            if (this.OtherSendCounter == uint.MaxValue - 1)
            {
                this.OtherSendCounter = 0;
                return;
            }

            this.OtherSendCounter++;
        }

        private void HandleReceived(FrameTcp tcpFrame)
        {
            Log.Info("{0} << {1}", this.Type, tcpFrame);
            this.OtherSendCounter = tcpFrame.SendCounter;
            this.IncrementOtherSendCounter();

            switch (tcpFrame.InformationId)
            {
                case InformationId.ErrorInformationIdUnknown:
                    Log.Warn("Unknown information Id");
                    break;
                case InformationId.ErrorProtocolIdUnknown:
                    Log.Warn("Unknown protocol Id");
                    break;
                case InformationId.SyncReq:
                    Log.Warn("Sync request received");
                    this.SendResponse(FrameVdS.CreateSyncRequestResponse(InformationId.SyncRes));
                    break;
                case InformationId.SyncRes:
                    Log.Warn("Sync response received");
                    this.KeyNumber = tcpFrame.KeyNumber;
                    break;
                case InformationId.PollReqRes:
                    Log.Warn("Polling request/response received");
                    this.LastPollReqReceived = DateTime.Now;
                    if (isServer)
                    {
                        break;
                    }

                    // client checks whether there is some data to transmit
                    var outFrames = new List<FrameVdS>();

                    if (!this.transmitQueue.Any())
                    {
                        this.SendResponse(FrameVdS.CreateEmpty(InformationId.PollReqRes));
                    }

                    FrameVdS outFrame;
                    if (!this.transmitQueue.TryDequeue(out outFrame))
                    {
                        throw new NullReferenceException("Frame missing");
                    }

                    outFrames.Add(outFrame);
                    var alertFrame = outFrame as FrameVdS_02;
                    if (alertFrame != null && alertFrame.MessageType == 0x73)
                    {
                        // check for measurement value changed
                        // another message is expected in the queue to indicate which measurement value has changed
                        if (!this.transmitQueue.TryDequeue(out outFrame))
                        {
                            throw new NullReferenceException("Text frame for measurement value change event is missing");
                        }

                        var asciiFrame = outFrame as FrameVdS_54;
                        if (asciiFrame == null)
                        {
                            throw new NullReferenceException();
                        }

                        // create a herstelleridmessage with text to report value change (customer idea!?!)
                        FrameVdS.CreateHerstellerIdMessage(asciiFrame.Text);
                        outFrames.Add(outFrame);
                    }
                    else
                    {
                        outFrames.Add(FrameVdS.CreateHerstellerIdMessage());
                    }

                    //< always add device id as last messages when data is transmitted
                    outFrames.Add(FrameVdS.CreateIdentificationNumberMessage());

                    this.SendResponse(outFrames.ToArray());

                    break;
                case InformationId.Payload:
                    Log.Warn("Payload received");

                    foreach (var frame in tcpFrame.Payload)
                    {
                        // Handle received frames
                        if (frame.VdsType == VdSType.Quittungsruecksendung)
                        {

                            var ackFrame = new FrameVdS_03(frame.Serialize(), 0);
                            if (ackFrame.MessageType != 0x00)
                            {
                                //TODO: check for ack
                            }

                            Log.Info("ACK received");
                            this.IsAcked = true;
                            this.NeedsAck = false;
                        }
                        else if (frame.VdsType == VdSType.Verbindung_wird_nicht_mehr_benoetigt)
                        {
                            this.IsAcked = true;
                        }
                        else
                        {
                            throw new NotImplementedException("Only Quittungsruecksendung supported");
                        }
                    }

                    break;
                default:
                    Log.Warn("Invalid Information ID");
                    break;
            }
        }

        private void SendResponse(params FrameVdS[] frames)
        {
            var response = new FrameTcp(
                this.MySendCounter,
                this.OtherSendCounter,
                this.KeyNumber,
                frames[0].InformationId,
                frames);
            Log.Info("{0} >> {1}", this.Type, response);

            var buff = response.Serialize();
            this.stream.Write(buff, 0, buff.Length);
            this.IncrementMySendCounter();
        }

        public void SendRequest(InformationId informationId)
        {
            switch (informationId)
            {
                case InformationId.SyncReq:
                    var syncReq = new FrameTcp(
                       this.MySendCounter,
                       this.OtherSendCounter,
                       this.KeyNumber,
                       informationId,
                       FrameVdS.CreateSyncRequestResponse(InformationId.SyncReq));
                    Log.Info("{0} >> {1}", this.Type, syncReq);

                    var syncReqbuff = syncReq.Serialize();
                    this.stream.Write(syncReqbuff, 0, syncReqbuff.Length);
                    this.IncrementMySendCounter();
                    break;
                case InformationId.SyncRes:
                    break;
                case InformationId.PollReqRes:
                    var pollReq = new FrameTcp(
                       this.MySendCounter,
                       this.OtherSendCounter,
                       this.KeyNumber,
                       informationId,
                       FrameVdS.CreateEmpty(InformationId.PollReqRes));
                    Log.Info("{0} >> {1}", this.Type, pollReq);

                    var polReqBuff = pollReq.Serialize();
                    this.stream.Write(polReqBuff, 0, polReqBuff.Length);
                    this.IncrementMySendCounter();
                    break;
                case InformationId.Payload:
                    break;
                case InformationId.ErrorInformationIdUnknown:
                    break;
                case InformationId.ErrorProtocolIdUnknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("intInformationId");
            }
        }
    }
}