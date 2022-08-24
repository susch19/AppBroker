﻿
using AppBroker.Core;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json.Linq;

using PainlessMesh;

namespace AppBrokerASP.Devices.Painless;

[DeviceName("ledstri")]
public partial class LedStrip : PainlessDevice
{
    [ObservableProperty]
    private string colorMode = "";

    [ObservableProperty]
    private int delay;

    [ObservableProperty]
    private int numberOfLeds;

    [ObservableProperty]
    private int brightness;

    [ObservableProperty]
    private uint step;

    [ObservableProperty]
    private bool reverse;

    [ObservableProperty]
    private uint colorNumber;

    [ObservableProperty]
    private ushort version;

    public LedStrip(long id, ByteLengthList parameter) : base(id, parameter)
    {
        ShowInApp = true;

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

        //var param = e.Parameters.FirstOrDefault();

        //logger.Debug(param.ToString());

        //var span = param.ToString().AsSpan();
        //var indices = span.IndexesOf(',');

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
        InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
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
        InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
    }
}
