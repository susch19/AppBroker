using Newtonsoft.Json;

using NLog;
using NLog.Targets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PainlessMesh.Ota;

public class UpdateManager : IUpdateManager
{
    public event EventHandler<FirmwareMetadata> Advertisment;

    readonly Dictionary<FirmwareId, FirmwareAdvertisment> currentAdvertisments = new();
    readonly Dictionary<FirmwareId, Timer> advertismentTimers = new();

    readonly Dictionary<string, FileOtaUpdate> fileOtas = new();
    private readonly FileSystemWatcher watcher;
    private readonly Logger logger;

    public UpdateManager()
    {
        _ = Directory.CreateDirectory("OTA");
        watcher = new FileSystemWatcher("OTA", "*.ota")
        {
            EnableRaisingEvents = true
        };
        watcher.Created += CreateAdvertismentFromFile;
        watcher.Changed += CreateAdvertismentFromFile;
        watcher.Deleted += StopAdvertismentFromFile;
        watcher.Renamed += StartStopAdvertismentFromFile;
        logger = NLog.LogManager.GetCurrentClassLogger();
        foreach (var path in Directory.GetFiles("OTA", "*.ota"))
        {
            CreateAdvertismentFromFile(path);

        }
    }

    private void CreateAdvertismentFromFile(object sender, FileSystemEventArgs e)
    {
        if (Path.GetExtension(e.FullPath) != ".ota")
        {
            logger.Info("Created file that didn't end with .ota, ignoring it: " + e.FullPath);
            return;
        }
        CreateAdvertismentFromFile(e.FullPath);
    }

    private void CreateAdvertismentFromFile(string path)
    {
        if (!File.Exists(path))
        {
            logger.Warn("Ota Metadata Path not existing. Path: " + path);

            return;
        }
        FileOtaUpdate otaUpdate;
        string content;
        try
        {
            content = File.ReadAllText(path);
            otaUpdate
                = JsonConvert.DeserializeObject<FileOtaUpdate>(content);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            return;
        }
        var fi = new FileInfo(otaUpdate.FilePath);
        if (!fi.Exists)
        {
            logger.Warn("Found ota metadata without existing firmware. Path: " + fi.FullName);
            return;
        }
        logger.Debug("Found ota metadata " + content);

        var metaData = otaUpdate.FirmwareMetadata;
        metaData.SizeInBytes = (uint)fi.Length;
        metaData.PackageCount = metaData.SizeInBytes / metaData.PartSize;

        if (metaData.SizeInBytes % metaData.PartSize > 0)
            metaData.PackageCount++;
        List<long> targets = new(otaUpdate.TargetIds);

        if (targets.Count <= 0)
        {
            targets.Add(0);
        }

        var dataBytes = File.ReadAllBytes(fi.FullName);

        List<byte[]> parts = new();

        for (int i = 0; i < metaData.PackageCount - 1; i++)
            parts.Add(ReadPart(dataBytes, metaData.PartSize, i, false));

        parts.Add(ReadPart(dataBytes, metaData.PartSize, 0, true));

        if (fileOtas.TryGetValue(path, out var val))
        {
            EndAdvertisingUpdate(val.FirmwareMetadata);
            _ = fileOtas.Remove(path);
        }
        fileOtas.Add(path, otaUpdate);
        foreach (var item in targets)
        {
            metaData = metaData with { TargetId = item };
            otaUpdate.FirmwareMetadata = metaData;

            AdvertiseUpdate(metaData, dataBytes, parts);
        }
    }
    static byte[] ReadPart(byte[] data, uint read, int i, bool rest)
    {
        byte[] part = new byte[read];
        int from = rest ? (int)(data.Length - (data.Length % read)) : (int)(read * i);
        int to = rest ? data.Length : (int)(read * (i + 1));
        return data[from..to];
    }

    private void StopAdvertismentFromFile(object sender, FileSystemEventArgs e) => StopAdvertismentFromFile(e.FullPath);
    private void StopAdvertismentFromFile(string path)
    {
        if (Path.GetExtension(path) != ".ota")
        {
            logger.Info("Deleted file that didn't end with .ota, ignoring it: " + path);
            return;
        }
        if (!fileOtas.TryGetValue(path, out var otaUpdate))
            return;

        EndAdvertisingUpdate(otaUpdate.FirmwareMetadata);
    }

    private void StartStopAdvertismentFromFile(object sender, RenamedEventArgs e)
    {
        StopAdvertismentFromFile(e.OldFullPath);
        CreateAdvertismentFromFile(e.FullPath);
    }

    public bool IsAdvertising(FirmwareMetadata metaData) => currentAdvertisments.ContainsKey(metaData.FirmwareId);
    public bool IsAdvertising(uint firmwareVersion, string deviceType, long targetId) => currentAdvertisments.ContainsKey(new(firmwareVersion, deviceType, targetId));

    public void AdvertiseUpdate(FirmwareMetadata metadata, byte[] data, List<byte[]> parts)
    {
        var firmwareId = metadata.FirmwareId;
        if (currentAdvertisments.TryGetValue(firmwareId, out var advertisment) && advertisment.AdvertiseUntil > DateTime.Now)
            return;

        currentAdvertisments[firmwareId] = new FirmwareAdvertisment(parts, DateTime.Now.AddHours(1), metadata, data);

        if (advertismentTimers.TryGetValue(firmwareId, out var timer))
            timer?.Dispose();
        logger.Debug("Advertising new Update for " + JsonConvert.SerializeObject(metadata));
        advertismentTimers[firmwareId] = new Timer(PushFirwmareUpdate, metadata, TimeSpan.FromDays(0), TimeSpan.FromMinutes(1));
    }

    private void PushFirwmareUpdate(object state)
    {
        if (state is not FirmwareMetadata metaData)
            throw new ArgumentException(nameof(state));

        Advertisment?.Invoke(this, metaData);
    }

    public byte[] GetPart(RequestFirmwarePart firmwarePart)
    {
        if (currentAdvertisments.TryGetValue(firmwarePart.FirmwareId, out var advertisment))
        {
            if (!firmwarePart.IncludeSize || firmwarePart.PartSize == 0)
            {
                return firmwarePart.PartNo > advertisment.Parts.Count
                    ? Array.Empty<byte>()
                    : advertisment.Parts[(int)firmwarePart.PartNo - 1];
            }
            var from = (int)((firmwarePart.PartNo - 1) * firmwarePart.PartSize);
            var to = (int)(firmwarePart.PartNo * firmwarePart.PartSize);
            if (to > advertisment.RawBytes.Length)
                to = advertisment.RawBytes.Length;
            return advertisment.RawBytes[from..to];

        }
        return Array.Empty<byte>();

    }

    public string GetPartBase64(RequestFirmwarePart firmwarePart)
    {
        if (currentAdvertisments.TryGetValue(firmwarePart.FirmwareId, out var advertisment))
        {
            if (firmwarePart.PartNo > advertisment.Parts.Count)
            {
                logger.Warn($"Got a PartNo {firmwarePart.PartNo} that was bigger than the advertisments max of {advertisment.Parts.Count}, returning empty");
                return "";
            }
            return Convert.ToBase64String(advertisment.Parts[(int)firmwarePart.PartNo - 1]);
        }
        logger.Warn("Got an unknown Request " + JsonConvert.SerializeObject(firmwarePart));

        return "";
    }

    public void EndAdvertisingUpdate(FirmwareMetadata firmwarePart)
    {
        var id = firmwarePart.FirmwareId;
        if (advertismentTimers.TryGetValue(id, out var timer))
        {
            timer?.Dispose();
            _ = advertismentTimers.Remove(id);
        }

        _ = currentAdvertisments.Remove(id);
    }
}
