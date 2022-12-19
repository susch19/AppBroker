namespace PainlessMesh.Ota;



internal partial struct FirmwareAdvertisment
{
    public List<byte[]> Parts { get; init; }
    public byte[] RawBytes { get; init; }
    public DateTime AdvertiseUntil { get; init; }
    public FirmwareMetadata Metadata { get; init; }

    public FirmwareAdvertisment(List<byte[]> parts, DateTime advertiseUntil, FirmwareMetadata metadata, byte[] rawBytes)
    {
        Parts = parts;
        AdvertiseUntil = advertiseUntil;
        Metadata = metadata;
        RawBytes = rawBytes;
    }
}
