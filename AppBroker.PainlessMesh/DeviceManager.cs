using System.Text;
using AppBroker.Core;

using AppBroker.Core.Managers;
using AppBroker.Core.Database;
using AppBrokerASP;

namespace AppBroker.PainlessMesh;

public class PainlessMeshDeviceManager
{
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly IDeviceManager deviceManager;

    public PainlessMeshDeviceManager()
    {
        deviceManager = InstanceContainer.Instance.DeviceManager;
        if (IInstanceContainer.Instance.TryGetDynamic(out SmarthomeMeshManager meshManager))
        {
            meshManager!.NewConnectionEstablished += Node_NewConnectionEstablished;
            meshManager.ConnectionLost += MeshManager_ConnectionLost;
            meshManager.ConnectionReestablished += MeshManager_ConnectionReastablished;
        }
    }


    private void MeshManager_ConnectionLost(object? sender, uint e)
    {
        if (deviceManager.Devices.TryGetValue(e, out var device))
        {
            logger.Info($"Lost connection to {device.FriendlyName} having nodeId {e}");
            device.StopDevice();
        }
    }
    private void MeshManager_ConnectionReastablished(object? sender, (uint id, ByteLengthList parameter) e)
    {
        if (deviceManager.Devices.TryGetValue(e.id, out var device))
        {
            logger.Info($"Reestablished connection to {device.FriendlyName} having nodeId {e}");
            device.Reconnect(e.parameter);
        }
    }

    private void Node_NewConnectionEstablished(object? sender, (Sub c, ByteLengthList l) e)
    {
        if (deviceManager.Devices.TryGetValue(e.c.NodeId, out var device))
        {
            logger.Info($"Reestablished connection with whoami to {device.FriendlyName} having nodeId {e}");
            device.Reconnect(e.l);
        }
        else
        {
            var deviceName = Encoding.UTF8.GetString(e.l[1]);
            var newDevice = IInstanceContainer.Instance.DeviceTypeMetaDataManager.CreateDeviceFromName(deviceName, null, e.c.NodeId, e.l);

            if (newDevice is null)
            {
                logger.Warn($"Couldn't create device for name {deviceName} having nodeId {e.c.NodeId}");
                return;
            }

            //_ = Devices.TryAdd(e.c.NodeId, newDevice);

            if (!DbProvider.AddDeviceToDb(newDevice))
                _ = DbProvider.MergeDeviceWithDbData(newDevice);

            logger.Debug($"New Device: {newDevice.TypeName}, {newDevice.Id}");
            deviceManager.AddNewDevice(newDevice);
        }
    }


}