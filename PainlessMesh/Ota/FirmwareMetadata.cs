using System;

namespace PainlessMesh.Ota
{
    public struct FirmwareMetadata : IEquatable<FirmwareMetadata>
    {
        public bool Forced { get; init; }
        public int FirmwareVersion { get; init; }
        public int SizeInBytes { get; init; }
        public int PartSize { get; init; }
        public int PackageCount { get; init; }
        public string DeviceType { get; init; }
        public short DeviceNr { get; init; }
        internal FirmwareId FirmwareId => new(FirmwareVersion, DeviceType);

        public FirmwareMetadata(bool forced, int firmwareVersion, int sizeInBytes, int partSize, int packageCount, string deviceType, short deviceNr)
        {
            Forced = forced;
            FirmwareVersion = firmwareVersion;
            SizeInBytes = sizeInBytes;
            PartSize = partSize;
            PackageCount = packageCount;
            DeviceType = deviceType;
            DeviceNr = deviceNr;
        }

        public override bool Equals(object obj) => obj is FirmwareMetadata metadata && Equals(metadata);
        public bool Equals(FirmwareMetadata other) => Forced == other.Forced && FirmwareVersion == other.FirmwareVersion && SizeInBytes == other.SizeInBytes && PartSize == other.PartSize && PackageCount == other.PackageCount && DeviceType == other.DeviceType && DeviceNr == other.DeviceNr;
        public override int GetHashCode() => HashCode.Combine(Forced, FirmwareVersion, SizeInBytes, PartSize, PackageCount, DeviceType, DeviceNr);

        public static bool operator ==(FirmwareMetadata left, FirmwareMetadata right) => left.Equals(right);
        public static bool operator !=(FirmwareMetadata left, FirmwareMetadata right) => !(left == right);

    }
}
