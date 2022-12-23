using System.Text;

namespace AppBroker.PainlessMesh.Ota;

public struct FileOtaUpdate
{

    public string FilePath { get; set; }
    public long[] TargetIds { get; set; }
    public FirmwareMetadata FirmwareMetadata { get; set; }

    public FileOtaUpdate(string filePath, long[] targetIds, FirmwareMetadata firmwareMetadata)
    {
        FilePath = filePath;
        TargetIds = targetIds;
        FirmwareMetadata = firmwareMetadata;
    }
}

public record struct FirmwareMetadata 
{
    public bool Forced { get; init; }
    public uint FirmwareVersion { get; init; }
    public uint SizeInBytes { get; set; }
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



    public byte[] ToBinary()
    {
        var bin = new byte[1 + 4 + 4 + 4 + 4 + 8 + 2];
        var binSpan = bin.AsSpan();
        _ = BitConverter.TryWriteBytes(binSpan, Forced);
        _ = BitConverter.TryWriteBytes(binSpan[1..], FirmwareVersion);
        _ = BitConverter.TryWriteBytes(binSpan[5..], SizeInBytes);
        _ = BitConverter.TryWriteBytes(binSpan[9..], PartSize);
        _ = BitConverter.TryWriteBytes(binSpan[13..], PackageCount);
        Encoding.ASCII.GetBytes(DeviceType).CopyTo(binSpan[17..25]);
        _ = BitConverter.TryWriteBytes(binSpan[25..], DeviceNr);
        return bin;
    }
}
