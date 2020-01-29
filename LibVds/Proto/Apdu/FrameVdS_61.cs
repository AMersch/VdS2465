using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibVds.Proto.Apdu
{
    public class FrameVdS_61 : FrameVdS
    {
        public enum Kennung : byte
        {
            Sonstige = 0x0,
            TcpIpLeitungsgebunden = 0x90,
            TcpIpFunk = 0x91
        }

        public enum Verbindungsart : byte
        {
            BedarfsgesteuerteVerbindung = 0x0,
            StehendeVerbindung = 0x1
        }

        // TODO
        public Kennung _Kennung
        {
            get { return (Kennung)0; }
        }

        // TODO
        public byte Verbindungsweg
        {
            get { return 0; }
        }

        // TODO
        public Verbindungsart _Verbindungsart
        {
            get { return (Verbindungsart)0; }
        }

        public FrameVdS_61(byte[] bytes, int start, InformationId informationId = InformationId.Payload)
            : base(bytes, start, informationId)
        {
        }

        public static FrameVdS_61 Create(Kennung kennung, byte verbindungsweg, Verbindungsart verbindungsart)
        {
            var buff = new byte[5];
            buff[0] = (byte)3;
            buff[1] = (byte)VdSType.Transportdienstkennung;
            buff[2] = (byte)kennung;
            buff[3] = verbindungsweg;
            buff[4] = (byte)verbindungsart;

            return new FrameVdS_61(buff, 0);
        }
    }
}