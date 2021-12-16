using System;
using System.Collections.Generic;

namespace PainlessMesh.Ota;



internal partial struct FirmwareAdvertisment
{
    public List<byte[]> Parts { get; init; }
    public DateTime AdvertiseUntil { get; init; }
    public FirmwareMetadata Metadata { get; init; }

    public FirmwareAdvertisment(List<byte[]> parts, DateTime advertiseUntil, FirmwareMetadata metadata)
    {
        Parts = parts;
        AdvertiseUntil = advertiseUntil;
        Metadata = metadata;
    }
}
