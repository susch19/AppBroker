
using AppBroker.Core;
using AppBroker.Core.Devices;

using Newtonsoft.Json;

using NonSucking.Framework.Extension.Threading;

using PainlessMesh;
using PainlessMesh.Ota;

using System.Reflection;
using System.Text;

namespace AppBrokerASP.Devices.Painless;

[AppBroker.ClassPropertyChangedAppbroker]
public abstract partial class PainlessDevice : PropChangedJavaScriptDevice
{
    private string iP = "";

    private uint firmwareVersionNr;
    [JsonProperty("version")]
    public string FirmwareVersion => "Firmware Version: " + FirmwareVersionNr;
    protected string LogName => Id + "/" + FriendlyName;
    private string deviceName;
    private DateTime lastPartRequestReceived;


    [AppBroker.IgnoreField]
    private static ScopedSemaphore updateSemaphore = new ScopedSemaphore(1);

    [AppBroker.IgnoreField]
    protected SmarthomeMeshManager meshManager;

    [AppBroker.IgnoreField]
    protected UpdateManager updateManager;


    protected PainlessDevice(long nodeId, string typeName) : base(nodeId, typeName, new FileInfo(Path.Combine("JSExtensionDevices", typeName + ".js")))
    {
        deviceName = GetType().GetCustomAttribute<DeviceNameAttribute>()?.PreferredName ?? TypeName;
        IInstanceContainer.Instance.TryGetDynamic(out SmarthomeMeshManager? mm);
        meshManager = mm!;
        IInstanceContainer.Instance.TryGetDynamic(out UpdateManager? um);
        updateManager = um!;

        meshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
        meshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
        meshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;


        Initialized = true;

    }

    protected PainlessDevice(long nodeId, ByteLengthList parameter, string typeName) : base(nodeId, typeName, new FileInfo(Path.Combine("JSExtensionDevices", typeName + ".js")))
    {
        deviceName = GetType().GetCustomAttribute<DeviceNameAttribute>()?.PreferredName ?? TypeName;
        InterpretParameters(parameter);
        IInstanceContainer.Instance.TryGetDynamic(out SmarthomeMeshManager? mm);
        meshManager = mm!;
        IInstanceContainer.Instance.TryGetDynamic(out UpdateManager? um);
        updateManager = um!;

        meshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
        meshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
        meshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;



    }

    private async void Node_SingleGetMessageReceived(object? sender, BinarySmarthomeMessage e)
    {
        if (e.NodeId != Id)
            return;
        Logger.Debug($"DataReceived in {nameof(Node_SingleGetMessageReceived)} {LogName}: " + e.ToJson());

        switch (e.Command)
        {
            case Command.Ota:
                var request = new RequestFirmwarePart(e.Parameters[0], Id);
                // Deserialize RequestFirmwarePart, send base64 as non json
                var b64 = updateManager.GetPart(request);
                if (b64.Length == 0)
                {
                    request.TargetId = 0;
                    b64 = updateManager.GetPart(request);
                }
                if (b64.Length == 0)
                {
                    Logger.Debug(LogName + " couldn't answer request " + request.ToJson());
                    break;
                }
                Logger.Debug(LogName + " answering ota request" + request.ToJson());
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

                using (updateSemaphore.Wait())
                {
                    meshManager.SendSingle((uint)Id, str);
                }


                break;
            case Command.OtaPart:
                break;
            default:
                break;
        }

        GetMessageReceived(e);
    }

    protected void Node_SingleUpdateMessageReceived(object? sender, BinarySmarthomeMessage e)
    {
        if (e.NodeId != Id)
            return;

        UpdateMessageReceived(e);
    }

    protected void Node_SingleOptionsMessageReceived(object? sender, BinarySmarthomeMessage e)
    {
        if (e.NodeId != Id)
            return;

        OptionMessageReceived(e);
    }

    public void OtaAdvertisment(FirmwareMetadata metadata)
    {
        if (!DeviceName.Equals(metadata.DeviceType, StringComparison.OrdinalIgnoreCase)
            || ((FirmwareVersionNr == metadata.FirmwareVersion || !metadata.Forced)
                && FirmwareVersionNr >= metadata.FirmwareVersion)
            || LastPartRequestReceived.AddSeconds(30) > DateTime.Now
            || (metadata.TargetId > 0 && metadata.TargetId != Id))
        {
            return;
        }

        Logger.Debug(LogName + $" v{FirmwareVersionNr} starting Ota with " + metadata.ToJson());

        //Do OTA
        var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Update, Command.Ota, metadata.ToBinary());
        meshManager.SendSingle((uint)Id, msg);
    }

    protected virtual void InterpretParameters(ByteLengthList parameter)
    {
        if (parameter.Count > 2)
        {
            try
            {
                IP = Encoding.UTF8.GetString(parameter[0]);

            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(InterpretParameters) + ": IP could not be read");
            }
            try
            {

                FirmwareVersionNr = BitConverter.ToUInt32(parameter[2]);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(InterpretParameters) + ": Firmware version could not be read, setting it to 1");
                FirmwareVersionNr = 1;
            }
        }
    }

    public override void StopDevice()
    {
        base.StopDevice();
        meshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
        meshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
        meshManager.SingleGetMessageReceived -= Node_SingleGetMessageReceived;
    }

    public override void Reconnect(ByteLengthList parameter)
    {
        base.Reconnect(parameter);

        InterpretParameters(parameter);

        meshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
        meshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
        meshManager.SingleGetMessageReceived -= Node_SingleGetMessageReceived;
        meshManager.SingleGetMessageReceived += Node_SingleGetMessageReceived;
        meshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
        meshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;

    }
    protected virtual void GetMessageReceived(BinarySmarthomeMessage e) { }

    protected virtual void UpdateMessageReceived(BinarySmarthomeMessage e) { }
    protected virtual void OptionMessageReceived(BinarySmarthomeMessage e) { }
}
