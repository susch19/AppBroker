using Newtonsoft.Json;

using PainlessMesh;
using PainlessMesh.Ota;

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Painless
{
    public abstract class PainlessDevice : Device
    {
        public string IP { get; protected set; }
        public int FirmwareVersion { get; protected set; }
        protected string LogName => Id + "/" + FriendlyName;

        protected DateTime LastPartRequestReceived { get; set; }

        protected PainlessDevice(long nodeId) : base(nodeId)
        {
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
        }


        protected PainlessDevice(long nodeId, List<string> parameter) : base(nodeId)
        {
            InterpretParameters(parameter);
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
        }

        private void Node_SingleGetMessageReceived(object? sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            logger.Debug($"DataReceived in {nameof(Node_SingleGetMessageReceived)} {LogName}: " + e.ToJson());

            switch (e.Command)
            {
                case Command.Ota:
                    var parameter = e.Parameters[0].ToString();
                    var request = new RequestFirmwarePart(Convert.FromBase64String(parameter), Id);
                    // Deserialize RequestFirmwarePart, send base64 as non json
                    var b64 = Program.UpdateManager.GetPartBase64(request);
                    if (string.IsNullOrEmpty(b64))
                    {
                        request.TargetId = 0;
                        b64 = Program.UpdateManager.GetPartBase64(request);
                    }
                    if (string.IsNullOrEmpty(b64))
                    {
                        logger.Debug(LogName + " couldn't answer request " + request.ToJson());
                        break;
                    }
                    logger.Debug(LogName + " answering ota request" + request.ToJson());
                    var str = Convert.ToBase64String(request.ToBinary()) + b64;
                    Task.Delay(Math.Min(str.Length / 2, 1000)).ContinueWith((r) => Program.MeshManager.SendSingle(Id, str));
                    
                    break;
                case Command.OtaPart:
                    break;
                default:
                    break;
            }

            GetMessageReceived(e);
        }


        protected void Node_SingleUpdateMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            logger.Debug($"DataReceived in {nameof(Node_SingleUpdateMessageReceived)} {LogName}: " + e.ToJson());

            UpdateMessageReceived(e);
        }


        protected void Node_SingleOptionsMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;

            logger.Debug($"DataReceived in {nameof(Node_SingleOptionsMessageReceived)} {LogName}: " + e.ToJson());

            OptionMessageReceived(e);
        }

        public void OtaAdvertisment(FirmwareMetadata metadata)
        {
            if (((FirmwareVersion == metadata.FirmwareVersion || !metadata.Forced)
                    && FirmwareVersion >= metadata.FirmwareVersion)
                || LastPartRequestReceived.AddSeconds(30) > DateTime.Now
                || (metadata.TargetId > 0 && metadata.TargetId != Id))
                return;

            logger.Debug(LogName + " starting Ota with " + metadata.ToJson());
            //DeviceType ���?�������?, FirmwareVersion 6678942, Forced 0, PartSize 0, Size 0, Cound 0, PartNo 1073740960

            //Do OTA
            var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Update, Command.Ota, Convert.ToBase64String(metadata.ToBinary()).ToJToken());
            Program.MeshManager.SendSingle((uint)Id, msg);
        }


        protected virtual void InterpretParameters(List<string> parameter)
        {
            if (parameter.Count > 2)
            {
                IP = parameter[0];

                FirmwareVersion = int.Parse(parameter[2]);
            }
        }

        public override void StopDevice()
        {
            base.StopDevice();
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived -= Node_SingleGetMessageReceived;
        }

        public override void Reconnect(List<string>? parameter)
        {
            base.Reconnect(parameter);

            InterpretParameters(parameter);

            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived -= Node_SingleGetMessageReceived;
            Program.MeshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;

        }
        protected virtual void GetMessageReceived(GeneralSmarthomeMessage e) { }

        protected virtual void UpdateMessageReceived(GeneralSmarthomeMessage e) { }
        protected virtual void OptionMessageReceived(GeneralSmarthomeMessage e) { }

    }
}
