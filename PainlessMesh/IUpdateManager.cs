
using System;
using System.IO;

namespace PainlessMesh.Ota;

public interface IUpdateManager
{
    event EventHandler<FirmwareMetadata> Advertisment;

    void AdvertiseUpdate(FirmwareMetadata metadata, Stream data);
    void EndAdvertisingUpdate(FirmwareMetadata firmwarePart);
    byte[] GetPart(RequestFirmwarePart firmwarePart);
    string GetPartBase64(RequestFirmwarePart firmwarePart);
    bool IsAdvertising(FirmwareMetadata metaData);
    bool IsAdvertising(uint firmwareVersion, string deviceType, long targetId);
}
