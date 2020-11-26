using AppBroker.Core.Devices;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.PainlessMesh
{
    class PainlessMeshDeviceManager : IDeviceManager
    {
        public ConcurrentDictionary<long, Device> Devices = new ConcurrentDictionary<long, Device>();
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private List<Type> types;
        Task temp;
        public DeviceManagerOld()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
            Program.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
            Program.MeshManager.ConnectionReastablished += MeshManager_ConnectionReastablished;
            temp = ConnectToIOBroker();
        }

        private void MeshManager_ConnectionLost(object sender, long e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.StopDevice();
            }
        }
        private void MeshManager_ConnectionReastablished(object sender, long e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.Reconnect();
            }
        }

        //private void DeviceReconnect((Sub c, List<string> l) e, List<Subscriber> clients)
        //{
        //    var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
        //    newDevice.Subscribers = clients;
        //    logger.Info($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
        //    while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
        //}

        private void Node_NewConnectionEstablished(object sender, (Sub c, List<string> l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                device.Reconnect();
            }
            else
            {

                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId, e.l);
                logger.Info($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                Devices.TryAdd(e.c.NodeId, newDevice);
                if (!DbProvider.AddDeviceToDb(newDevice))
                    DbProvider.MergeDeviceWithDbData(newDevice);
            }
        }
    }
}
