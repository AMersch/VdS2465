using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibVds.Proto.Apdu
{
    public class FrameVdS_58 : FrameVdS
    {
        public FrameVdS_58(byte[] bytes, int start, InformationId informationId = InformationId.Payload)
            : base(bytes, start, informationId)
        {
        }

        // TODO
        public Guid GeraeteUuid
        {
            get { return Guid.Empty; }
        }

        public static FrameVdS_58 Create(Guid uuid)
        {
            var bytes = uuid.ToByteArray();

            var buff = new byte[bytes.Length + 3];
            buff[0] = (byte)(buff.Length - 2);
            buff[1] = (byte)VdSType.Geraete_UUID;
            buff[2] = (byte)bytes.Length;
            Array.Copy(bytes, 0, buff, 3, bytes.Length);

            return new FrameVdS_58(buff, 0);
        }
    }
}