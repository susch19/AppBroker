using PainlessMesh;
using PainlessMesh.Ota;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Painless
{
    public abstract class PainlessDevice : Device
    {
        public string IP { get; protected set; }
        public int FirmwareVersion { get; protected set; }

        protected DateTime LastPartRequestReceived { get; set; }

        protected PainlessDevice(long nodeId) : base(nodeId)
        {
        }

        protected PainlessDevice(long nodeId, List<string> parameter) : base(nodeId)
        {
            InterpretParameters(parameter);
        }

        public void OtaAdvertisment(FirmwareMetadata metadata)
        {
            if (((FirmwareVersion == metadata.FirmwareVersion || !metadata.Forced)
                    && FirmwareVersion >= metadata.FirmwareVersion)
                || LastPartRequestReceived.AddSeconds(30) > DateTime.Now)
                return;


            //Do OTA
            var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Update, Command.OTA, metadata.ToJToken());
            Program.MeshManager.SendCustomSingle((uint)Id, PackageType.OTA_ANNOUNCE, msg);

        }

        protected virtual void InterpretParameters(List<string> parameter)
        {
            if (parameter.Count > 2)
            {
                IP = parameter[0];

                FirmwareVersion = int.Parse(parameter[0]);
            }
        }
    }
}
