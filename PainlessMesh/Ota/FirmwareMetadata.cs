using Newtonsoft.Json.Converters;

using System;
using System.Text;

namespace PainlessMesh.Ota
{
    public struct FileOtaUpdate
    {
        public string FilePath { get; set; }
        public FirmwareMetadata FirmwareMetadata { get; set; }
    }

    public struct FirmwareMetadata : IEquatable<FirmwareMetadata>
    {
        public bool Forced { get; init; }
        public uint FirmwareVersion { get; init; }
        public uint SizeInBytes { get;  set; }
        public uint PartSize { get; init; }
        public uint PackageCount { get; set; }
        public string DeviceType { get; init; }
        public ushort DeviceNr { get; init; }
        internal FirmwareId FirmwareId => new(FirmwareVersion, DeviceType, TargetId);
        public long TargetId { get; set; }

        public FirmwareMetadata(bool forced, uint firmwareVersion, uint sizeInBytes, uint partSize, uint packageCount, string deviceType, ushort deviceNr)
        {
            Forced = forced;
            FirmwareVersion = firmwareVersion;
            SizeInBytes = sizeInBytes;
            PartSize = partSize;
            PackageCount = packageCount;
            DeviceType = deviceType.PadRight('\0');
            DeviceNr = deviceNr;
            TargetId = 0;
        }

        public FirmwareMetadata(Span<byte> data)
        {
            Forced = BitConverter.ToBoolean(data);
            FirmwareVersion = BitConverter.ToUInt32(data[1..]);
            SizeInBytes = BitConverter.ToUInt32(data[5..]);
            PartSize = BitConverter.ToUInt32(data[9..]);
            PackageCount = BitConverter.ToUInt32(data[13..]);
            DeviceType = Encoding.ASCII.GetString(data[17..]);
            DeviceNr = BitConverter.ToUInt16(data[25..]);
            TargetId = 0;

        }

        public override bool Equals(object obj) => obj is FirmwareMetadata metadata && Equals(metadata);
        public bool Equals(FirmwareMetadata other) => Forced == other.Forced && FirmwareVersion == other.FirmwareVersion && SizeInBytes == other.SizeInBytes && PartSize == other.PartSize && PackageCount == other.PackageCount && DeviceType == other.DeviceType && DeviceNr == other.DeviceNr;
        public override int GetHashCode() => HashCode.Combine(Forced, FirmwareVersion, SizeInBytes, PartSize, PackageCount, DeviceType, DeviceNr);

        public static bool operator ==(FirmwareMetadata left, FirmwareMetadata right) => left.Equals(right);
        public static bool operator !=(FirmwareMetadata left, FirmwareMetadata right) => !(left == right);

        public byte[] ToBinary()
        {
            var bin = new byte[1 + 4 + 4 + 4 + 4 + 8 + 2];
            var binSpan = bin.AsSpan();
            BitConverter.TryWriteBytes(binSpan, Forced);
            BitConverter.TryWriteBytes(binSpan[1..], FirmwareVersion);
            BitConverter.TryWriteBytes(binSpan[5..], SizeInBytes);
            BitConverter.TryWriteBytes(binSpan[9..], PartSize);
            BitConverter.TryWriteBytes(binSpan[13..], PackageCount);
            Encoding.ASCII.GetBytes(DeviceType).CopyTo(binSpan[17..25]);
            BitConverter.TryWriteBytes(binSpan[25..], DeviceNr);
            return bin;
        }


    }
}
