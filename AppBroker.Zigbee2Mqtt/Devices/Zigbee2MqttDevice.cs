using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;

using AppBrokerASP;
using AppBrokerASP.Devices;

using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NiL.JS.Core;

using NLog;

using Org.BouncyCastle.Asn1.Cmp;

namespace AppBroker.Zigbee2Mqtt.Devices;

public partial class Zigbee2MqttDevice : PropChangedJavaScriptDevice
{
    internal record SetFeatureValue(Zigbee2MqttGenericExposedFeature Feature, object Value);

    protected static readonly IManagedMqttClient client;
    internal readonly Zigbee2MqttDeviceJson device;
    protected static readonly Zigbee2MqttManager zigbeeManager;
    private readonly ILogger logger = LogManager.GetCurrentClassLogger();
    private Dictionary<string, Zigbee2MqttGenericExposedFeature> cachedFeatures = new();

    public override bool IsConnected
    {
        get
        {
            if (IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue(Id, "available") is bool b)
                return b;
            return base.IsConnected;
        }
        set => base.IsConnected = value;
    }

    public string LastReceivedFormatted
    {
        get
        {
            var stateFromManager =
                IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue(Id, "lastReceived");
            if (stateFromManager is DateTime dt)
                return dt.ToString("dd.MM.yyyy HH:mm:ss");

            return "No Data";
        }
    }

    static Zigbee2MqttDevice()
    {
        zigbeeManager = IInstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>();
        client = IInstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>().MQTTClient!;
    }

    public Zigbee2MqttDevice(Zigbee2MqttDeviceJson device, long id)
        : base(id, GetTypeName(device), new FileInfo(Path.Combine("JSExtensionDevices", $"{GetTypeName(device)}.js")))
    {
        this.device = device;

        ShowInApp = true;
        FriendlyName = device.FriendlyName;
    }
    public Zigbee2MqttDevice(Zigbee2MqttDeviceJson device, long id, string typeName)
        : base(id, typeName, new FileInfo(Path.Combine("JSExtensionDevices", $"{typeName}.js")))
    {
        this.device = device;

        ShowInApp = true;
        FriendlyName = device.FriendlyName;
    }

    public static string GetTypeName(Zigbee2MqttDeviceJson d)
        => d.Definition?.Model ?? d.ModelId ?? d.Type.ToString();

    public async Task FetchCurrentData()
    {
        await client.EnqueueAsync($"zigbee2mqtt/{Id}/get", @"{""state"": """"}");
    }


    internal Task<bool> SetValue(SetFeatureValue value)
    {
        return SetValues(new Dictionary<string, object>() { { value.Feature.Property, value.Value } });
    }

    internal Task<bool> SetValues(IEnumerable<SetFeatureValue> values)
    {
        return SetValues(values.Where(CanSetValue).ToDictionary(x => x.Feature.Property, x => x.Value));
    }

    /// <inheritdoc/>
    public override void ReceivedNewState(string name, JToken newValue, StateFlags stateFlags)
    {
        zigbeeManager.SetValue(this, name, newValue);
    }

    private bool CanSetValue(SetFeatureValue value)
    {
        if (!value.Feature.Access.HasFlag(Zigbee2MqttFeatureAccessMode.Write))
        {
            // TODO: log warning, eg. for light type it has child features which you can set
            logger.Warn($"Couldn't set value {value} on {FriendlyName}, because it was not writable");
            return false;
        }

        if (!value.Feature.ValidateValue(value))
        {
            // TODO: log warning
            logger.Warn($"Couldn't set value {value} on {FriendlyName}, because the value was not valid");
            return false;
        }

        return true;
    }

    private async Task<bool> SetValues(Dictionary<string, object> values)
    {
        if (!values.Any())
            return false;

        await client.EnqueueAsync($"zigbee2mqtt/{device.FriendlyName}/set", JsonConvert.SerializeObject(values));
        return true;
    }

    private void InvokeOnDevice<T>(Action<IEnumerable<T>> action) where T : Zigbee2MqttGenericExposedFeature
    {
        if (device?.Definition?.Exposes is null)
            return;

        action(device.Definition.Exposes.OfType<T>());
    }

    private void InvokeOnDevice(string property, Action<Zigbee2MqttGenericExposedFeature> action)
    {
        if (device?.Definition?.Exposes is null)
            return;

        if (cachedFeatures.TryGetValue(property, out var feature))
        {
            action(feature);
            return;
        }

        var toFind = property.Split('.');

        Zigbee2MqttGenericExposedFeature? lastFound =
            device
            .Definition
            .Exposes.FirstOrDefault(x => x.Name == toFind[0])
            ??
            device
            .Definition
            .Exposes.FirstOrDefault(x => x.Type.ToString() == toFind[0])
            ;

        int index = 1;

        do
        {
            if (lastFound is null)
            {
                logger.Warn($"Couldn't find {property} on {FriendlyName}");
                return;
            }

            lastFound = lastFound.Features.FirstOrDefault(x => x.Name == toFind[index])
                ?? lastFound.Features.FirstOrDefault(x => x.Type.ToString() == toFind[index]);
        } while (++index < toFind.Length);

        if (lastFound is null)
        {
            logger.Warn($"Couldn't find {property} on {FriendlyName}");
            return;
        }

        cachedFeatures[property] = lastFound;
        action(lastFound);
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Zigbee:
                var name = parameters[0].ToString();
                object? value = parameters[1].ToOObject();
                if (value is null)
                    return Task.CompletedTask;

                InvokeOnDevice(name, async x => await SetValue(new SetFeatureValue(x, value)));
                break;
            case (Command)150:
            {
                var propName = parameters[0].ToString();
                zigbeeManager.SetValue(this, propName, parameters[1]);
                break;
            }
            case (Command)151:
            {
                var propName = parameters[1].ToString();
                zigbeeManager.SetValue(this, propName, parameters[0]);
                break;
            }
            case (Command)152:
            {
                var propName = parameters[0].ToString();
                var deviceId = parameters[2].ToObject<long>();
                zigbeeManager.SetValue(deviceId, propName, parameters[1]);
                break;
            }
            case (Command)153:
            {
                var propName = parameters[1].ToString();
                var deviceId = parameters[2].ToObject<long>();
                zigbeeManager.SetValue(deviceId, propName, parameters[0]);
                break;
            }
        }
        return base.UpdateFromApp(command, parameters);
    }

    protected override Context ExtendEngine(Context engine)
    {
        logger.Debug($"Extending engine on {FriendlyName}");
        engine.DefineFunction("invokeOnDevice", (string name, object value) => InvokeOnDevice(name, (x) => SetValue(new SetFeatureValue(x, value))));

        return base.ExtendEngine(engine);
    }

    protected override bool FriendlyNameChanging(string oldName, string newName)
    {
#if DEBUG
        return false;
#endif
        if (string.IsNullOrWhiteSpace(newName))
            return false;
        try
        {
            if (string.IsNullOrWhiteSpace(oldName))
            {
                zigbeeManager.friendlyNameToIdMapping[newName] = Id;
                return true;
            }
            logger.Info($"Trying to rename {oldName} to {newName} for device with id {Id}");
            client.EnqueueAsync("zigbee2mqtt/bridge/request/device/rename", $"{{\"from\": \"{oldName}\", \"to\": \"{newName}\"}}");
            if (zigbeeManager.friendlyNameToIdMapping.TryGetValue(oldName, out var id)
                  && id == Id
                  && !zigbeeManager.friendlyNameToIdMapping.ContainsKey(newName)
                  && zigbeeManager.friendlyNameToIdMapping.Remove(oldName, out _))
            {
                zigbeeManager.friendlyNameToIdMapping[newName] = id;
                return true;
            }

            logger.Error($"Couldn't rename {oldName} to {newName} for zigbee2mqtt, because the old name was incorrect, it couldn't be removed from the mapping or the new name was already used");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $" Couldn't rename {oldName} to {newName} for zigbee2mqtt");
        }
        return false;
    }
}
