using System;
using System.Collections.Generic;
using System.Text;

namespace LibVds.Proto.Apdu
{
    public class FrameVdS_57 : FrameVdS
    {
        public FrameVdS_57(byte[] bytes, int start, InformationId informationId = InformationId.Payload)
            : base(bytes, start, informationId)
        {
        }

        public byte GMA
        {
            // TODO
            get { return 0; }
        }
        
        public string Seriennummer
        {
            // TODO
            get { return String.Empty; }
        }

        public string Hersteller
        {
            // TODO
            get { return String.Empty; }
        }

        public string GeraeteTyp
        {
            // TODO
            get { return String.Empty; }
        }

        public string FirmwareVersion
        {
            // TODO
            get { return String.Empty; }
        }

        public static FrameVdS_57 Create(byte gmaNr, string seriennummer, string hersteller, string geraeteTyp, string firmwareVersion)
        {
            var dataSeriennummer = Encoding.ASCII.GetBytes(seriennummer);
            var dataGeraeteHersteller = Encoding.ASCII.GetBytes(hersteller);
            var dataGeraeteTyp = Encoding.ASCII.GetBytes(geraeteTyp);
            var dataFirmwareVersion = Encoding.ASCII.GetBytes(firmwareVersion);

            var buff = new List<byte>(dataSeriennummer.Length +
                dataGeraeteHersteller .Length +
                dataGeraeteTyp.Length +
                dataFirmwareVersion .Length + 7);

            buff.Add((byte)(buff.Capacity - 2));
            buff.Add((byte)VdSType.Geraete_Identifikation);
            buff.Add(gmaNr);
            buff.Add((byte)dataSeriennummer.Length);
            buff.AddRange(dataSeriennummer);
            buff.Add((byte)dataGeraeteHersteller.Length);
            buff.AddRange(dataGeraeteHersteller);
            buff.Add((byte)dataGeraeteTyp.Length);
            buff.AddRange(dataGeraeteTyp);
            buff.Add((byte)dataFirmwareVersion.Length);
            buff.AddRange(dataFirmwareVersion);

            return new FrameVdS_57(buff.ToArray(), 0);
        }
    }
}