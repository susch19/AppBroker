
using AppBroker;
using AppBroker.Core;
using AppBroker.Core.Devices;
using Newtonsoft.Json.Linq;

using AppBroker.PainlessMesh;

using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace AppBrokerASP.Devices.Painless;

[DeviceName("ledstri")]
public partial class LedStrip : PainlessDevice
{
    public string ColorMode
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    public int Delay
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }

    public int NumberOfLeds
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }

    public int Brightness
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }

    public uint Step
    {
        get => GetProperty<uint>();
        set => SetProperty(value);
    }

    public bool Reverse
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    public uint ColorNumber
    {
        get => GetProperty<uint>();
        set => SetProperty(value);
    }

    public ushort Version
    {
        get => GetProperty<ushort>();
        set => SetProperty(value);
    }


    private readonly SmarthomeMeshManager? smarthomeMeshManager;

    public LedStrip(long id, ByteLengthList parameter) : base(id, parameter, "PainlessMeshLedStrip")
    {
        ShowInApp = true;
        IInstanceContainer.Instance.TryGetDynamic(out smarthomeMeshManager);
    }

    public override void ReceivedNewState(string name, JToken newValue, StateFlags stateFlags)
    {
        var command = Command.Zigbee;
        var messageType = MessageType.Update;
        ByteLengthList meshParams = new();

        switch (name)
        {
            case "colorMode":
                var strValue = newValue.ToObject<string>();
                if (Enum.TryParse<Command>(strValue, out var cmd))
                {
                    command = cmd;
                }
                break;
            case "brightness":
                messageType = MessageType.Options;
                command = Command.Brightness;
                meshParams.Add(BitConverter.GetBytes(newValue.ToObject<int>()));
                break;
            case "delay":
                messageType = MessageType.Options;
                command = Command.Delay;
                meshParams.Add(BitConverter.GetBytes(newValue.ToObject<int>()));
                break;
            case "numberOfLeds":
                messageType = MessageType.Options;
                command = Command.Calibration;
                meshParams.Add(BitConverter.GetBytes(newValue.ToObject<int>()));
                break;
            case "colorNumber":
                messageType = MessageType.Options;
                command = Command.Color;
                meshParams.Add(BitConverter.GetBytes(newValue.ToObject<uint>()));
                break;
            default:
                break;
        }

        if (command == Command.Zigbee)
            return;

        var msg = new BinarySmarthomeMessage((uint)Id, messageType, command, meshParams);
        smarthomeMeshManager?.SendSingle((uint)Id, msg);
    }


    private void SetProperty<T>(T value, [CallerMemberName] string propertyName = "")
    {
        SetState(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], JToken.FromObject(value));

    }
    private T? GetProperty<T>([CallerMemberName] string propertyName = "")
    {

        var state = InstanceContainer.Instance.DeviceStateManager.GetSingleState(Id, char.ToLowerInvariant(propertyName[0]) + propertyName[1..]);
        if (state is null)
            return default;

        return state.ToObject<T>();
    }

    protected override void OptionMessageReceived(BinarySmarthomeMessage e) { }
    protected override void UpdateMessageReceived(BinarySmarthomeMessage e)
    {
        if (e.Command != Command.Mode)
            return;

        for (int i = 0; i < e.Parameters.Count; i++)
        {
            byte[]? item = e.Parameters[i];
            if (item is null)
                continue;
            if (i == 0)
                ColorMode = System.Text.Encoding.UTF8.GetString(item);
            else if (i == 1)
                Delay = BitConverter.ToInt32(item);
            else if (i == 2)
                NumberOfLeds = BitConverter.ToInt32(item);
            else if (i == 3)
                Brightness = BitConverter.ToInt32(item);
            else if (i == 4)
                Step = BitConverter.ToUInt32(item);
            else if (i == 5)
                Reverse = BitConverter.ToBoolean(item);
            else if (i == 6)
                ColorNumber = BitConverter.ToUInt32(item);
            else if (i == 7)
                Version = BitConverter.ToUInt16(item);
            ;
        }

        SendDataToAllSubscribers();
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        ByteLengthList meshParams = new();
        switch (command)
        {
            case Command.SingleColor:
                if (parameters.Count > 0)
                {
                    uint color = Convert.ToUInt32(parameters[0].ToString(), 16);
                    meshParams.Add(BitConverter.GetBytes(color));
#if DEBUG
                    this.ColorNumber = color;
                    SendDataToAllSubscribers();
#endif
                }
                break;
            default:
                break;
        }

        var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Update, command, meshParams);
        smarthomeMeshManager?.SendSingle((uint)Id, msg);
        return Task.CompletedTask;
    }

    public override void OptionsFromApp(Command command, List<JToken> parameters)
    {
        ByteLengthList meshParams = new();
        switch (command)
        {
            case Command.SingleColor:
            case Command.Color:
                if (parameters.Count > 0)
                {

                    uint color = Convert.ToUInt32(parameters[0].ToString(), 16);
                    meshParams.Add(BitConverter.GetBytes(color));
#if DEBUG
                    this.ColorNumber = color;
                    SendDataToAllSubscribers();
#endif
                }
                break;
            case Command.Brightness:
            case Command.Calibration:
            case Command.Delay:
                if (parameters.Count > 0)
                {
                    int color = Convert.ToInt32(parameters[0].ToString(), 16);
                    meshParams.Add(BitConverter.GetBytes(color));

#if DEBUG
                    this.ColorNumber = (uint)color;
                    SendDataToAllSubscribers();
#endif
                }
                break;
            default:
                break;
        }

        var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Options, command, meshParams);
        smarthomeMeshManager?.SendSingle((uint)Id, msg);
    }

}
