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

        public bool IsAdvertising(FirmwareMetadata metaData) => currentAdvertisments.ContainsKey(metaData.FirmwareId);
        public bool IsAdvertising(int firmwareVersion, string deviceType) => currentAdvertisments.ContainsKey(new(firmwareVersion, deviceType));

        public void AdvertiseUpdate(FirmwareMetadata metadata, Stream data)
        {
            var firmwareId = metadata.FirmwareId;
            if (currentAdvertisments.TryGetValue(firmwareId, out var advertisment) && advertisment.AdvertiseUntil > DateTime.Now)
                return;

            static void ReadPart(Stream data, List<byte[]> parts, int read)
            {
                byte[] part = new byte[read];
                data.Read(part);
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
                return advertisment.Parts[firmwarePart.PartNo];
            }
            return Array.Empty<byte>();

        }

        public string GetPartBase64(RequestFirmwarePart firmwarePart)
        {
            if (currentAdvertisments.TryGetValue(firmwarePart.FirmwareId, out var advertisment))
            {
                if (firmwarePart.PartNo > advertisment.Parts.Count)
                    return "";
                return Convert.ToBase64String(advertisment.Parts[firmwarePart.PartNo]);
            }
            
            return "";
        }

        public void EndAdvertisingUpdate(FirmwareMetadata firmwarePart)
        {
            var id = firmwarePart.FirmwareId;
            if (advertismentTimers.TryGetValue(id, out var timer)) 
            {
                timer?.Dispose();
                advertismentTimers.Remove(id);
            }

            currentAdvertisments.Remove(id);
        }
    }
}
