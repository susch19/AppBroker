using Newtonsoft.Json;

using NLog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PainlessMesh.Ota
{
    public class UpdateManager
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
            var content = File.ReadAllText(path);
            var otaUpdate = JsonConvert.DeserializeObject<FileOtaUpdate>(content);
            if (!File.Exists(otaUpdate.FilePath))
            {
                logger.Warn("Found ota metadata without existing firmware. Path: " + otaUpdate.FilePath);
                return;
            }
            logger.Debug("Found ota metadata " + content);

            var fi = new FileInfo(otaUpdate.FilePath);
            var metaData = otaUpdate.FirmwareMetadata;
            metaData.SizeInBytes = (uint)fi.Length;
            metaData.PackageCount = metaData.SizeInBytes / metaData.PartSize;

            if (metaData.SizeInBytes % metaData.PartSize > 0)
                metaData.PackageCount++;
            otaUpdate.FirmwareMetadata = metaData;
            if (fileOtas.TryGetValue(path, out var val))
            {
                EndAdvertisingUpdate(val.FirmwareMetadata);
                _ = fileOtas.Remove(path);
            }

            fileOtas.Add(path, otaUpdate);
            using FileStream str = File.OpenRead(otaUpdate.FilePath);
            AdvertiseUpdate(metaData, str);
        }

        private void StopAdvertismentFromFile(object sender, FileSystemEventArgs e)
        {
            StopAdvertismentFromFile(e.FullPath);
        }
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

        public void AdvertiseUpdate(FirmwareMetadata metadata, Stream data)
        {
            var firmwareId = metadata.FirmwareId;
            if (currentAdvertisments.TryGetValue(firmwareId, out var advertisment) && advertisment.AdvertiseUntil > DateTime.Now)
                return;

            static void ReadPart(Stream data, List<byte[]> parts, uint read)
            {
                byte[] part = new byte[read];
                _ = data.Read(part);
                parts.Add(part);
            }

            List<byte[]> parts = new();

            for (int i = 0; i < metadata.PackageCount - 1; i++)
                ReadPart(data, parts, metadata.PartSize);

            var rest = ((metadata.SizeInBytes - 1) % metadata.PartSize) + 1;
            ReadPart(data, parts, rest);

            currentAdvertisments[firmwareId] = new FirmwareAdvertisment(parts, DateTime.Now.AddHours(1), metadata);

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
                if (firmwarePart.PartNo > advertisment.Parts.Count)
                    return Array.Empty<byte>();
                return advertisment.Parts[(int)firmwarePart.PartNo - 1];
            }
            return Array.Empty<byte>();

        }

        public string GetPartBase64(RequestFirmwarePart firmwarePart)
        {
            if (currentAdvertisments.TryGetValue(firmwarePart.FirmwareId, out var advertisment))
            {
                if (firmwarePart.PartNo > advertisment.Parts.Count)
                {
                    logger.Warn($"Got a PartNo {firmwarePart.PartNo} that was bigger than the advertisments max of { advertisment.Parts.Count}, returning empty");
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
}
