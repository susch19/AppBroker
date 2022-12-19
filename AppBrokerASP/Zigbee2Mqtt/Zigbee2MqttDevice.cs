using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;

using AppBrokerASP.Devices;

using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NiL.JS.Core;

using Quartz;

using System.Globalization;
using System.Xml.Linq;


namespace AppBrokerASP.Zigbee2Mqtt;

public class Zigbee2MqttDevice : ConnectionJavaScriptDevice
{
    internal record SetFeatureValue(Zigbee2MqttGenericExposedFeature Feature, object Value);

    internal readonly Zigbee2MqttDeviceJson device;
    private static readonly IManagedMqttClient client;


    private Dictionary<string, Zigbee2MqttGenericExposedFeature> cachedFeatures = new();

    static Zigbee2MqttDevice()
    {
        client = InstanceContainer.Instance.Zigbee2MqttManager.MQTTClient!;
    }

    internal Zigbee2MqttDevice(Zigbee2MqttDeviceJson device, long id)
        : base(id, GetTypeName(device), new FileInfo(Path.Combine("JSExtensionDevices", $"{GetTypeName(device)}.js")))
    {
        this.device = device;

        ShowInApp = true;
        FriendlyName = device.FriendlyName;
    }

    public static string GetTypeName(Zigbee2MqttDeviceJson d) 
        => d.Definition?.Model ?? d.ModelId ?? d.Type.ToString();

    public override void SetState(string name, JToken newValue)
    {
        base.SetState(name, newValue);

        object? value = newValue.ToOObject();

        if (value is null)
            return;

        client.EnqueueAsync($"zigbee2mqtt/{device.FriendlyName}/set/name", newValue.ToString());
    }

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

    private static bool CanSetValue(SetFeatureValue value)
    {
        if (!value.Feature.Access.HasFlag(Zigbee2MqttFeatureAccessMode.Write))
        {
            // TODO: log warning, eg. for light type it has child features which you can set
            return false;
        }

        if (!value.Feature.ValidateValue(value))
        {
            // TODO: log warning
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
                return; //TODO Log not found

            lastFound = lastFound.Features.FirstOrDefault(x => x.Name == toFind[index])
                ?? lastFound.Features.FirstOrDefault(x => x.Type.ToString() == toFind[index]);
        } while (++index < toFind.Length);

        if (lastFound is null)
            return; //TODO Log not found

        //var features = device
        //    .Definition
        //    .Exposes
        //    .SelectMany(x => x.Features)
        //    .Where(x => x is not null)
        //    .Where(x => string.Equals(x.Name, property, StringComparison.OrdinalIgnoreCase));
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
        }
        return base.UpdateFromApp(command, parameters);
    }

    protected override Context ExtendEngine(Context engine)
    {
        engine.DefineFunction("invokeOnDevice", (string name, object value) => InvokeOnDevice(name, (x) => SetValue(new SetFeatureValue(x, value))));

        return base.ExtendEngine(engine);
    }
}
