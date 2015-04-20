namespace LibVds.Proto.Apdu
{
    /// <summary>
    /// Zustands�nderung, Steuerung mit Quittungsanforderung
    /// </summary>
    /// <remarks>
    /// Mit diesem Satztyp werden Zustands�nderungen und Steuerbefehle �bertragen, diue von der Empf�ngerseite eine Telegrammquittung (Typ 0x03) anfordern.
    /// </remarks>
    public class FrameVdS_02 : FrameVdS
    {
        public FrameVdS_02(byte[] bytes, int start, InformationId informationId = InformationId.Payload) : base(bytes, start, informationId)
        {
        }

        public byte Address
        {
            get { return this.buffer[3]; }
        }

        public byte AddressAdd
        {
            get { return this.buffer[4]; }
        }

        public byte AddressExt
        {
            get { return this.buffer[5]; }
        }
    }
}