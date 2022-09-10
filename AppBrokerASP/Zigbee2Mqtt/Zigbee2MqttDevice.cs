using AppBroker.Core;
using AppBroker.Core.Devices;

using AppBrokerASP.Devices;

using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Globalization;


namespace AppBrokerASP.Zigbee2Mqtt;

public class Zigbee2MqttDevice : ConnectionJavaScriptDevice
{
    internal record SetFeatureValue(GenericExposedFeature Feature, object Value);

    private readonly Device device;
    private readonly IManagedMqttClient client;


    internal Zigbee2MqttDevice(Device device, IManagedMqttClient client)
        : base(long.Parse(device.IEEEAddress[2..], NumberStyles.HexNumber), device.ModelId, new FileInfo(Path.Combine("JSExtensionDevices", device.ModelId + ".js")))
    {
        this.device = device;
        this.client = client;

        TypeName = device.ModelId;
        ShowInApp = true;
        FriendlyName = device.FriendlyName;
    }

    public async Task FetchCurrentData()
    {
        await client.EnqueueAsync($"zigbee2mqtt/{Id}/get", @"{""state"": """"}");
    }

    internal Task SetValue(SetFeatureValue value)
    {
        return SetValues(new Dictionary<string, object>() { { value.Feature.Property, value } });
    }

    internal Task SetValues(SetFeatureValue[] values)
    {
        return SetValues(values.Where(CanSetValue).ToDictionary(x => x.Feature.Property, x => x.Value));
    }


    private static bool CanSetValue(SetFeatureValue value)
    {
        if (!value.Feature.Access.HasFlag(FeatureAccessMode.Write))
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

    private async Task SetValues(Dictionary<string, object> values)
    {
        if (!values.Any())
            return;

        await client.EnqueueAsync($"zigbee2mqtt/{Id}/set", JsonConvert.SerializeObject(values));
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        return base.UpdateFromApp(command, parameters);
    }
}
