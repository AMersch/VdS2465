using System;
using System.Text;

namespace LibVds.Proto.Apdu
{
    public class FrameVdS_50 : FrameVdS
    {
        public FrameVdS_50(byte[] bytes, int start, InformationId informationId = InformationId.Payload)
            : base(bytes, start, informationId)
        {
        }

        public DateTime DatumUhrzeit
        {
            // TODO
            get { return DateTime.Now; }
        }

        public static FrameVdS_50 Create(DateTime datumUhrzeit)
        {
            var jahr = byte.Parse(datumUhrzeit.ToString("yy"));
            var jahrhundert = (byte)((int.Parse(datumUhrzeit.ToString("yyyy")) - jahr) / 100);
            var monat = byte.Parse(datumUhrzeit.ToString("MM"));
            var tag = byte.Parse(datumUhrzeit.ToString("dd"));
            var stunde = byte.Parse(datumUhrzeit.ToString("HH"));
            var minute = byte.Parse(datumUhrzeit.ToString("mm"));
            var sekunde = byte.Parse(datumUhrzeit.ToString("ss"));
            byte msb = 0;
            byte lsb = 0;

            var data = new[] { jahr, jahrhundert, monat, tag, stunde, minute, sekunde, msb, lsb };
            var buff = new byte[data.Length + 2];
            buff[0] = (byte)data.Length;
            buff[1] = (byte)VdSType.Datum_Uhrzeit;
            Array.Copy(data, 0, buff, 2, data.Length);

            return new FrameVdS_50(buff, 0);
        }
    }
}