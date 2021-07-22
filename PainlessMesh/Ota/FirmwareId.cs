using System;

namespace PainlessMesh.Ota
{
    internal struct FirmwareId : IEquatable<FirmwareId>
    {
        public int FirmwareVersion { get; init; }
        public string DeviceType { get; init; }

        public FirmwareId(int firmwareVersion, string deviceType)
        {
            FirmwareVersion = firmwareVersion;
            DeviceType = deviceType;
        }

        public override bool Equals(object obj) => obj is FirmwareId id && Equals(id);
        public bool Equals(FirmwareId other) => FirmwareVersion == other.FirmwareVersion && DeviceType == other.DeviceType;
        public override int GetHashCode() => HashCode.Combine(FirmwareVersion, DeviceType);

        public static bool operator ==(FirmwareId left, FirmwareId right) => left.Equals(right);
        public static bool operator !=(FirmwareId left, FirmwareId right) => !(left == right);
    }
}
