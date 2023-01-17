using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;
using AppBroker.Zigbee2Mqtt;

using AppBrokerASP;

using NLog;

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace AppBroker.maxi;

internal class Plugin : IPlugin
{
    public string Name => "Maxi Private Plugin";
    private readonly SerialDisposable serialDisposable;
    private Logger mainLogger;

    public Plugin()
    {
        serialDisposable = new SerialDisposable();
        mainLogger = LogManager.CreateNullLogger();
    }

    public bool Initialize(LogFactory logFactory)
    {
        mainLogger = logFactory.GetLogger(Name);
        mainLogger.Info("Start to initalize plugin");

        var livingRoomDevices = new[] { 0x00158d0002389da1, 0x00158d00053d2ba1, 0x00158d0002ab36dd, 0x00158d00054d9840, 0x00158d0002a1d9c2 };
        var kitchenDevices = new[] { 0x00158d000238a35e, 0x00158d00053d2bb8, 0x00158d0002a1e224 };
        var bathDevices = new[] { 0x00158d0002775c7e, 0x00158d0001ff8e1e, 0x00158d0002a1e1a8 };
        var bedroomDevices = new[] { 0x00158d000349bf07, 0x00158d00053d2b33, 0x00158d0002ab319a };
        var corridorDevices = new[] { 0x00158d000349c41a, 0x00158d00054d9f9e };

        var zigbeeManager = InstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>();
        var deviceManager = IInstanceContainer.Instance.DeviceManager;


        var deviceContext = new PluginFactory.DeviceContext(livingRoomDevices, kitchenDevices, bathDevices, bedroomDevices, corridorDevices);

        serialDisposable
            .Disposable
                = PluginFactory
                .Create(mainLogger, zigbeeManager, deviceManager, deviceContext)
                .Subscribe();

        mainLogger.Info("Initialize successfull");

        return true;
    }
}
