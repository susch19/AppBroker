namespace PainlessMesh.Ota
{
    public struct RequestFirmwarePart
    {

        public int FirmwareVersion { get; init; }
        public string DeviceType { get; init; }
        public int PartNo { get; init; }
        //uint16_t deviceNr; Implement
        internal FirmwareId FirmwareId => new(FirmwareVersion, DeviceType);

        public RequestFirmwarePart(int firmwareVersion, string deviceType, int partNo)
        {
            FirmwareVersion = firmwareVersion;
            DeviceType = deviceType;
            PartNo = partNo;
        }

    }
}
