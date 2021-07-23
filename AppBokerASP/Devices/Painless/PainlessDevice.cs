using Newtonsoft.Json;

using PainlessMesh;
using PainlessMesh.Ota;

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Painless
{
    public abstract class PainlessDevice : Device
    {
        public string IP { get; protected set; }
        public uint FirmwareVersionNr { get; set; }
        public string FirmwareVersion => "Firmware Version: " + FirmwareVersionNr;
        protected string LogName => Id + "/" + FriendlyName;

        protected DateTime LastPartRequestReceived { get; set; }

        protected PainlessDevice(long nodeId) : base(nodeId)
        {
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
        }


        protected PainlessDevice(long nodeId, ByteLengthList parameter) : base(nodeId)
        {
            InterpretParameters(parameter);
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
        }

        private void Node_SingleGetMessageReceived(object? sender, BinarySmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            logger.Debug($"DataReceived in {nameof(Node_SingleGetMessageReceived)} {LogName}: " + e.ToJson());

            switch (e.Command)
            {
                case Command.Ota:
                    var request = new RequestFirmwarePart(e.Parameters[0], Id);
                    // Deserialize RequestFirmwarePart, send base64 as non json
                    var b64 = Program.UpdateManager.GetPart(request);
                    if (b64.Length == 0)
                    {
                        request.TargetId = 0;
                        b64 = Program.UpdateManager.GetPart(request);
                    }
                    if (b64.Length == 0)
                    {
                        logger.Debug(LogName + " couldn't answer request " + request.ToJson());
                        break;
                    }
                    logger.Debug(LogName + " answering ota request" + request.ToJson());
                    var header = new SmarthomeHeader(1, SmarthomePackageType.Ota);
                    byte[] str;
                    using (var memoryStream = new MemoryStream())
                    {
                        header.Serialize(memoryStream);
                        var req = request.ToBinary();
                        memoryStream.WriteSpan<byte>(req.AsSpan(), req.Length, false);
                        memoryStream.WriteSpan<byte>(b64.AsSpan(), b64.Length, false);
                        str = memoryStream.ToArray();
                    }

                    Program.MeshManager.SendSingle((uint)Id, str);

                    break;
                case Command.OtaPart:
                    break;
                default:
                    break;
            }

            GetMessageReceived(e);
        }


        protected void Node_SingleUpdateMessageReceived(object sender, BinarySmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            logger.Debug($"DataReceived in {nameof(Node_SingleUpdateMessageReceived)} {LogName}: " + e.ToJson());

            UpdateMessageReceived(e);
        }


        protected void Node_SingleOptionsMessageReceived(object sender, BinarySmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;

            logger.Debug($"DataReceived in {nameof(Node_SingleOptionsMessageReceived)} {LogName}: " + e.ToJson());

            OptionMessageReceived(e);
        }

        public void OtaAdvertisment(FirmwareMetadata metadata)
        {
            if (((FirmwareVersionNr == metadata.FirmwareVersion || !metadata.Forced)
                    && FirmwareVersionNr >= metadata.FirmwareVersion)
                || LastPartRequestReceived.AddSeconds(30) > DateTime.Now
                || (metadata.TargetId > 0 && metadata.TargetId != Id))
                return;

            logger.Debug(LogName + " starting Ota with " + metadata.ToJson());
            //DeviceType ���?�������?, FirmwareVersion 6678942, Forced 0, PartSize 0, Size 0, Cound 0, PartNo 1073740960

            //Do OTA
            var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Update, Command.Ota, metadata.ToBinary());
            Program.MeshManager.SendSingle((uint)Id, msg);
        }


        protected virtual void InterpretParameters(ByteLengthList parameter)
        {
            if (parameter.Count > 2)
            {
                IP = Encoding.UTF8.GetString(parameter[0]);

                FirmwareVersionNr = BitConverter.ToUInt32(parameter[2]);
            }
        }

        public override void StopDevice()
        {
            base.StopDevice();
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleGetMessageReceived -= Node_SingleGetMessageReceived;
        }

        public override void Reconnect(ByteLengthList? parameter)
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
        protected virtual void GetMessageReceived(BinarySmarthomeMessage e) { }

        protected virtual void UpdateMessageReceived(BinarySmarthomeMessage e) { }
        protected virtual void OptionMessageReceived(BinarySmarthomeMessage e) { }

    }
}
