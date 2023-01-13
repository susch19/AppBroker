using AppBroker.Core;
using AppBroker.Core.Devices;

using AppBrokerASP;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

using SocketIOClient.Transport;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.PainlessMesh;
public class PainlessMeshMqttManager : IAsyncDisposable
{
    public IManagedMqttClient? MQTTClient { get; set; }

    private readonly PainlessMeshSettings config;
    private readonly string topic;
    private readonly SmarthomeMeshManager meshManager;
    private readonly Lazy<PainlessMeshDeviceManager> meshDeviceManager = new(IInstanceContainer.Instance.GetDynamic<PainlessMeshDeviceManager>);
    private readonly Logger logger;
    private Dictionary<long, ByteLengthList> whoAmIMessages = new();

    public PainlessMeshMqttManager(PainlessMeshSettings painlessMeshConfig, SmarthomeMeshManager meshManager)
    {
        config = painlessMeshConfig;
        topic = painlessMeshConfig.MQTTTopic;
        this.meshManager = meshManager;
        logger = LogManager.GetCurrentClassLogger();
        meshManager.NewConnectionEstablished += MeshManager_NewConnectionEstablished;
    }

    private void MeshManager_NewConnectionEstablished(object? sender, (long nodeId, ByteLengthList) e)
    {
        whoAmIMessages[e.nodeId] = e.Item2;
        if (MQTTClient is not null)
            MQTTClient.EnqueueAsync(topic + "/bridge/devices", JsonConvert.SerializeObject(whoAmIMessages), retain: true);
    }

    public async Task Subscribe()
    {
        if (MQTTClient is null)
            return;

        try
        {
            logger.Debug($"Subscribing to {topic} topic");
            await MQTTClient.SubscribeAsync($"{topic}/#");

        }
        catch (Exception ex)
        {
            logger.Error($"Erorr during subscribing to {topic} topic", ex);
        }
    }

    internal string GetHexRepresentation(long value) => string.Format("0x{0:x16}", value);


    public async Task<bool> SetValue(long deviceId, string propertyName, JToken newValue)
    {
        if (MQTTClient is null || IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device))
            return false;

        await MQTTClient.EnqueueAsync($"{topic}/{GetHexRepresentation(deviceId)}/set/{propertyName}", newValue.ToString());
        return true;
    }
    public Task<bool> SetValue(Device device, string propertyName, JToken newValue)
    {
        if (MQTTClient is null)
            return Task.FromResult(false);
        return SetValue(device.Id, propertyName, newValue);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">The sub path to push the message to, "topic/" is already part of it</param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public Task EnqueueToMqtt(string path, JToken payload)
    {
        return EnqueueToMqtt($"{topic}/{path}", payload.ToString());
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">The sub path to push the message to, "topic/id/" is already part of it</param>
    /// <param name="id"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public Task EnqueueToMqtt(string path, long id, JToken payload)
    {
        return EnqueueToMqtt($"{topic}/{GetHexRepresentation(id)}/{path}", payload.ToString());
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">The sub path to push the message to, "topic/" is already part of it</param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public Task EnqueueToMqtt(string path, string payload)
    {
        return MQTTClient.EnqueueAsync($"{topic}/{path}", payload);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">The sub path to push the message to, "topic/id/" is already part of it</param>
    /// <param name="id"></param>
    /// 
    /// <param name="payload"></param>
    /// <returns></returns>
    public Task EnqueueToMqtt(string path, long id, string payload)
    {
        return EnqueueToMqtt($"{topic}/{GetHexRepresentation(id)}/{path}", payload);
    }


    public Task EnqueueToMqtt(BinarySmarthomeMessage message)
    {
        return MQTTClient.EnqueueAsync($"{topic}/{GetHexRepresentation(message.NodeId)}/message", JsonConvert.SerializeObject(message));
    }

    public async Task<IManagedMqttClient> Connect()
    {
        logger.Debug("Connecting to mqtt");
        if (MQTTClient is not null)
        {

            logger.Debug("Already connected to mqtt, returing existing instance");
            return MQTTClient;
        }

        var mqttFactory = new MqttFactory();
        var managedMqttClient = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer(config.MQTTAddress, config.MQTTPort)
                        .WithClientId(config.MQTTClientId + Guid.NewGuid().ToString())
                        .Build();
        logger.Debug("Builded new mqtt tcp server options");

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();
        logger.Debug("Builded new mqtt tcp client options");

        MQTTClient = managedMqttClient;
        MQTTClient.ApplicationMessageReceivedAsync += Mqtt_ApplicationMessageReceivedAsync;
        logger.Debug("Subscribed the incomming mqtt messages");
        await managedMqttClient.StartAsync(managedMqttClientOptions);
        logger.Debug("Started the mqtt client");

        return MQTTClient;
    }

    private async Task Mqtt_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (!topic.StartsWith($"{this.topic}/"))
        {
            return;
        }

        var payload = e.ApplicationMessage.ConvertPayloadToString();
        if (payload is null)
            return;

        var splitted = topic.Split('/');

        var method = splitted[2];
        var idStr = splitted[1];
        long id;
        if (idStr.Equals("bridge", StringComparison.OrdinalIgnoreCase))
            id = 1;
        else
            id = Convert.ToInt64(idStr, 16);

        switch (method)
        {
            case "devices":
                var whoAmIMessages = JsonConvert.DeserializeObject<Dictionary<long, ByteLengthList>>(payload);
                if (whoAmIMessages is null)
                    return;
                this.whoAmIMessages = whoAmIMessages;

                foreach (var item in whoAmIMessages)
                {
                    if (!IInstanceContainer.Instance.DeviceManager.Devices.ContainsKey(item.Key))
                        meshDeviceManager.Value.Node_NewConnectionEstablished(this, (item.Key, item.Value));
                    else
                        meshDeviceManager.Value.MeshManager_ConnectionReastablished(this, ((uint)item.Key, item.Value));
                }
                break;
            case "network":
                var network = JsonConvert.DeserializeObject<Sub>(payload);
                if (network is null)
                    return;
                meshManager.RefreshMesh(network);
                break;
            case "message":
                if (meshManager.ConnectedClients > 0)
                    return;

                var raw = JsonConvert.DeserializeObject<BinarySmarthomeMessage>(payload);
                if (raw is null || raw.Command == Command.Mesh)
                    return;
                meshManager.SocketClientDataReceived(raw);
                break;
            case "logging":
                break;
            case "state":
                if (splitted.Length == 3)
                {
                    TryInterpretTopicAsStateUpdate(id, payload);
                }
                else if(splitted.Length == 4 && splitted[3] == "set")
                {
                    var singleState = JToken.Parse(payload).First;
                    SetSingleState(id, singleState.Path, singleState.First);
                }
                else if(splitted.Length == 5 && splitted[3] == "set")
                {
                    var propName = splitted[4];
                    SetSingleState(id, propName, payload);
                }

                break;

        }
    }

    private void TryInterpretTopicAsStateUpdate(long id, string payload)
    {
        InstanceContainer
            .Instance
            .DeviceStateManager
            .SetMultipleStates(id, JsonConvert.DeserializeObject<Dictionary<string, JToken>>(payload)!);

        InstanceContainer
            .Instance
            .DeviceStateManager
            .SetSingleState(id, "lastReceived", DateTime.Now);
    }
    private void SetSingleState(long id, string propName, JToken value)
    {
        InstanceContainer
            .Instance
            .DeviceStateManager
            .SetSingleState(id, propName, value, StateFlags.All);

        InstanceContainer
            .Instance
            .DeviceStateManager
            .SetSingleState(id, "lastReceived", DateTime.Now);
    }

    public async ValueTask DisposeAsync()
    {
        var local = MQTTClient;
        if (local is not null)
        {
            await local.StopAsync();
            local.Dispose();
        }
    }
}
