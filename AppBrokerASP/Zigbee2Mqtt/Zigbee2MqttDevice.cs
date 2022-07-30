using AppBroker.Core;

using Elsa.Models;

using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Npgsql.Replication;

using System.Globalization;
using System.Text;

using BrokerDevice = AppBroker.Core.Devices.Device;

namespace AppBrokerASP.Zigbee2Mqtt;

public class DeviceStateManager
{
    private  ConcurrentDictionary<long, Dictionary<string, JToken>> deviceStates = new();

    public  bool ManagesDevice(long id) => deviceStates.ContainsKey(id);

    //TODO Router or Mainspower get from Zigbee2Mqtt
    public Dictionary<string, JToken>? GetCurrentState(long id)
    {
        deviceStates.TryGetValue(id, out var result);
        return result;
    }

    public bool TryGetCurrentState(long id, out Dictionary<string, JToken>? result)
        => deviceStates.TryGetValue(id, out result);

    //TODO History of states
    public  void PushNewState(long id, Dictionary<string, JToken> newState)
    {
        deviceStates[id] = newState;
        if(InstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device))
            device.SendDataToAllSubscribers();
        
    }
}

public class Zigbee2MqttDevice : BrokerDevice
{
    private readonly Device device;
    private readonly IManagedMqttClient client;
    private Dictionary<string, JToken> state = new();

    public override long Id { get; set; }
    public override string TypeName { get; set; }
    public override bool ShowInApp { get; set; }
    public override string FriendlyName { get; set; }
    public override bool IsConnected { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken>? State => InstanceContainer.Instance.DeviceStateManager.GetCurrentState(Id);

    internal Zigbee2MqttDevice(Device device, IManagedMqttClient client)
        : base(long.Parse(device.IEEEAddress[2..], NumberStyles.HexNumber))
    {
        this.device = device;
        this.client = client;

        TypeName = device.ModelId;
        ShowInApp = true;
        FriendlyName = device.FriendlyName;
        IsConnected = true;
        client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
    }

    private Task Client_ApplicationMessageReceivedAsync(MQTTnet.Client.MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;

        if (!topic.EndsWith(device.IEEEAddress))
            return Task.CompletedTask;

        state = JsonConvert.DeserializeObject<Dictionary<string, JToken>>( e.ApplicationMessage.ConvertPayloadToString())!;

        return Task.CompletedTask;
    }


    public async Task FetchCurrentData()
    {
        await client.EnqueueAsync($"zigbee2mqtt/{Id}/get", @"{""state"": """"}");
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        return base.UpdateFromApp(command, parameters);
    }
}
