using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Models;

using Esprima.Ast;

using Newtonsoft.Json.Linq;

using System.Text;


namespace AppBroker.Zigbee2Mqtt.Devices;
[Flags]
internal enum DayOfWeekF
{
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
    VacationOrAway = 1 << 7,
}

internal class Setting
{
    public DayOfWeekF DayOfWeek { get; set; }
    public ushort Time { get; set; }
    public short Temperature { get; set; }

    public Setting()
    {

    }
    public Setting(Span<byte> bytes)
    {
        DayOfWeek = (DayOfWeekF)bytes[0];
        Time = (ushort)(bytes[2] << 8 | bytes[1]);
        Temperature = (short)(bytes[4] << 8 | bytes[3]);
    }
}

[DeviceName("SuschHeater")]
internal class SuschHeater : Zigbee2MqttDevice
{
    public SuschHeater(Zigbee2MqttDeviceJson device, long id) : base(device, id)
    {
    }

    public SuschHeater(Zigbee2MqttDeviceJson device, long id, string typeName) : base(device, id, typeName)
    {
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Temp:
                float temp = (float)parameters[0];
                await zigbeeManager.SetValue(Id, "heating_setpoint", temp);
                break;
            case Command.Mode:
                var val = parameters[0].ToString();
                await zigbeeManager.SetValue(Id, "system_mode", val);
                    break;
            default:
                await base.UpdateFromApp(command, parameters);
                break;
        }
    }

    public override void OptionsFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Temp:
            {
                var configs = parameters.Skip(1).Select(x => x.ToDeObject<HeaterConfig>()).GroupBy(x => new { x.TimeOfDay, x.Temperature });

                List<Setting> settings = new List<Setting>();

                foreach (var item in configs)
                {
                    var newSetting = new Setting();
                    var key = item.Key;
                    newSetting.Temperature = (short)(key.Temperature * 100);
                    newSetting.Time = (ushort)(key.TimeOfDay.TimeOfDay.Hours * 60 + key.TimeOfDay.TimeOfDay.Minutes);
                    foreach (var hc in item)
                    {
                        newSetting.DayOfWeek |= (DayOfWeekF)(1 << ((byte)(hc.DayOfWeek + 1) % 7));
                    }
                    settings.Add(newSetting);
                }

                StringBuilder builder = new StringBuilder($$"""
                    {
                        "transitions": {{settings.Count}},
                        "mode": 0
                    """);

                for (int i = 0; i < 10; i++)
                {
                    builder.Append(',');
                    int dow = 0;
                    int transitionTime = 0;
                    int setPoint = 0;
                    if (settings.Count > i)
                    {
                        var item = settings[i];
                        dow = (int)item.DayOfWeek;
                        transitionTime = item.Time;
                        setPoint = item.Temperature;
                    }
                    builder.Append(
                        $$"""
                            "day_of_week_{{i + 1}}": {{dow}},
                            "transition_time_{{i + 1}}": {{transitionTime}},
                            "set_point_{{i + 1}}": {{setPoint}}
                            """
                        );
                }
                builder.Append("}");

                zigbeeManager.SetCommand(Id, 0xff00, 0xff, builder.ToString());
                break;
            }
        }

        SendDataToAllSubscribers();
    }

    internal override Dictionary<string, JToken> ConvertStates(Dictionary<string, JToken> customStates)
    {
        if (customStates.TryGetValue("current_target_num", out var curTargetNum) 
            && curTargetNum.ToObject<uint>() is uint curTarget)
        {
            var temp = (curTarget >> 16);
            var time = (int)((curTarget >> 4) & 0xFFF);
            var dayOfWeek = (curTarget & 0xF);

            customStates["current_target_temp"] = temp / 100d;
            customStates["current_target_time"] = new DateTime(DateOnly.MinValue, new TimeOnly(time / 60, time % 60), DateTimeKind.Utc);
            customStates["current_target_dow"] = dayOfWeek;
        }
        if(customStates.TryGetValue("running_mode", out var runningMode))
        {
            customStates["running_mode_b"] = runningMode.ToString() != "off";
        }

        return customStates;
    }

    public override dynamic? GetConfig()
    {
        var currentConfig = GetState("current_config");
        if (currentConfig is null)
            return null;

        var data = Convert.FromBase64String(currentConfig.ToString());

        var numberOfTransitions = data[2];
        var settings = new Setting[numberOfTransitions];
        for (int i = 0; i < numberOfTransitions; i++)
        {
            settings[i] = new(data.AsSpan()[(4 + i * 5)..]);
        }

        List<HeaterConfig> configs = new List<HeaterConfig>();

        foreach (var item in settings)
        {
            var dow = (int)item.DayOfWeek;
            for (int i = 0; i < 7; i += 1)
            {
                if (((1 << i) & dow) > 0)
                {
                    configs.Add(new HeaterConfig((Core.Models.DayOfWeek)((i + 6) % 7), new DateTime(DateOnly.MinValue, new TimeOnly(item.Time / 60, item.Time % 60)), item.Temperature / 100d));
                }
            }

        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(configs);
    }
}
