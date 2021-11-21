using System;

namespace PainlessMesh.Ota;

internal struct FirmwareId : IEquatable<FirmwareId>
{
    public uint FirmwareVersion { get; init; }
    public string DeviceType { get; init; }
    public long TargetId { get; set; }

    public FirmwareId(uint firmwareVersion, string deviceType, long targetId)
    {
        FirmwareVersion = firmwareVersion;
        DeviceType = deviceType;
        TargetId = targetId;
    }

    public override bool Equals(object obj) => obj is FirmwareId id && Equals(id);
    public bool Equals(FirmwareId other) => FirmwareVersion == other.FirmwareVersion && DeviceType == other.DeviceType && TargetId == other.TargetId;
    public override int GetHashCode() => HashCode.Combine(FirmwareVersion, DeviceType, TargetId);

    public static bool operator ==(FirmwareId left, FirmwareId right) => left.Equals(right);
    public static bool operator !=(FirmwareId left, FirmwareId right) => !(left == right);
}
