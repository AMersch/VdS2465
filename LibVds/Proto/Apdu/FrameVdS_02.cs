namespace LibVds.Proto.Apdu
{
    /// <summary>
    /// Meldung Zustands�nderung, Steuerung mit Quittungsanforderung
    /// </summary>
    /// <remarks>
    /// Mit diesem Satztyp werden Zustands�nderungen und Steuerbefehle �bertragen, die von der Empf�ngerseite eine Telegrammquittung (Typ 0x03) anfordern.
    /// </remarks>
    public class FrameVdS_02 : FrameVdS
    {
        public FrameVdS_02(byte[] bytes, int start, InformationId informationId = InformationId.Payload) : base(bytes, start, informationId)
        {
        }

        public byte Device
        {
            get { return this.buffer[2]; }
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

        public byte MessageType
        {
            get { return this.buffer[6]; }
        }

        /// <summary>
        /// Zustands�nderungen und Steuerbefehle die vom Empf�nger eine Telegrammquittung Typ 0x03 anfordern.
        /// </summary>
        /// <param name="device">�G hat Ger�tenummer 0, erste angeschlossenen Zentrale hat Ger�tenummer 1.</param>
        /// <param name="address">0x00 bedeutet gesamte Zentrale.</param>
        /// <param name="addressAdd">0x00 bedeutet Zustands�nderung bezieht sich auf gesamte Adresse.</param>
        /// <param name="addressExt">0x01: Meldeing�nge/Me�werte.
        ///                          0x02: Schaltausg�nge/Stellwerte
        ///                          Das h�herwertige Half-Byte kann als Adress-Offset benutzt werden.</param>
        /// <param name="messageType">Bit 7: Ein/Aus, 1->Ruhe, Aus, zur�ckgenommen, unscharf
        ///                                           0->Alarm, Ein, ausgel�st, scharf
        ///                           Bit6-4: Meldungsblock
        ///                                   0x01: Allgemein: Meldung/Schaltzustand/Steuern
        ///                                   0x02: �berfall/Einbruch
        ///                                   0x03: St�rungsmeldung
        ///                                   0x04: Sonstige Meldung
        ///                                   0x05: Ger�temeldungen
        ///                                   0x06: Zustandsmeldungen
        ///                                   0x07: Firmenspezifische Meldungen
        ///                                   --> weitere Details: S.31
        ///                           Bit3-0: Meldungskennung</param>
        /// <returns>A VDS frame</returns>
        public static FrameVdS_02 Create(byte device, byte address, byte addressAdd, byte addressExt, byte messageType)
        {
            var buff = new byte[]
                           {
                               0x00, 
                               (byte)VdSType.Meldung_Zustandsaenderung__Steuerung_mit_Quittungsanforderung, 
                               device,
                               address, 
                               addressAdd, 
                               addressExt, 
                               messageType
                           };
            buff[0] = (byte)(buff.Length - 2);
            return new FrameVdS_02(buff, 0, InformationId.Payload);
        }
    }
}