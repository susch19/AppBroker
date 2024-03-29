﻿using Newtonsoft.Json;

using System.Text;

namespace AppBroker.PainlessMesh.Ota;

public struct RequestFirmwarePart
{

    public uint FirmwareVersion { get; init; }
    public string DeviceType { get; init; }
    public uint PartNo { get; init; }
    //uint16_t deviceNr; Implement
    [JsonIgnore]
    internal FirmwareId FirmwareId => new(FirmwareVersion, DeviceType, TargetId);
    [JsonIgnore]
    public long TargetId { get; set; }
    public uint PartSize { get; set; }
    public bool IncludeSize { get; private set; }

    public RequestFirmwarePart(uint firmwareVersion, string deviceType, uint partNo, long targetId = 0)
    {
        FirmwareVersion = firmwareVersion;
        DeviceType = deviceType;

        PartNo = partNo;
        TargetId = targetId;
    }

    public RequestFirmwarePart(Span<byte> data, long targetId = 0)
    {
        FirmwareVersion = BitConverter.ToUInt32(data);
        DeviceType = System.Text.Encoding.ASCII.GetString(data[4..12]).Trim('\0');
        PartNo = BitConverter.ToUInt32(data[12..]);
        IncludeSize = data.Length >= 20;
        if (IncludeSize)
        {
            PartSize = BitConverter.ToUInt32(data[16..]);
            
        }
        TargetId = targetId;
    }

    public byte[] ToBinary()
    {
        var ret = new byte[IncludeSize ? 20 : 16];
        var span = ret.AsSpan();
        _ = BitConverter.TryWriteBytes(span, FirmwareVersion);
        Encoding.UTF8.GetBytes(DeviceType).CopyTo(span[4..]);
        _ = BitConverter.TryWriteBytes(span[12..], PartNo);
        if (IncludeSize)
            BitConverter.TryWriteBytes(span[16..], PartSize);
        return ret;
    }
}
