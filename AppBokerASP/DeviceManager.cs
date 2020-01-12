using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Devices;
using AppBokerASP.IOBroker;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;
using SimpleSocketIoClient;

namespace AppBokerASP
{
    public class DeviceManager
    {
        public ConcurrentDictionary<ulong, Device> Devices = new ConcurrentDictionary<ulong, Device>();
        private SocketIoClient client;

        private List<Type> types;
        Task temp;
        public DeviceManager()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
            Program.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
            temp = ConnectToIOBroker();
        }

        private void MeshManager_ConnectionLost(object sender, uint e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                device.IsConnected = false;
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.StopDevice();
            }
        }

        private void DeviceReconnect((Sub c, List<string> l) e, List<Subscriber> clients)
        {
            var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
            newDevice.Subscribers = clients;
            Console.WriteLine($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
            while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
        }

        private void Node_NewConnectionEstablished(object sender, (Sub c, List<string> l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                device.IsConnected = true;
                device.Reconnect();
                //while (!Devices.TryRemove(e.c.NodeId, out var d)) { }
                //device.StopDevice();
                //DeviceReconnect(e, device.Subscribers);
                //device = null;
            }
            else
            {

                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
                Console.WriteLine($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
                if (!DbProvider.AddDeviceToDb(newDevice))
                    DbProvider.MergeDeviceWithDbData(newDevice);
                //using var cont = DbProvider.BrokerDbContext;
                //if(!cont.Devices.Any(x => x.Id == e.c.NodeId))
                //{
                //    cont.Devices.Add(newDevice);
                //    cont.SaveChanges();
                //}
            }
        }

        private async Task ConnectToIOBroker()
        {

            client = new SocketIoClient();

            client.AfterEvent += (sender, args) =>
                {
                    var suc = IoBrokerZigbee.TryParse(args.Value, out var zo);
                    if (suc)
                    {
                        if (!Devices.TryGetValue(zo.Id, out var dev))
                        {
                            dev = new XiaomiTempSensor(zo.Id);
                            while (!Devices.TryAdd(zo.Id, dev)) { }
                            if (!DbProvider.AddDeviceToDb(dev))
                                DbProvider.MergeDeviceWithDbData(dev);
                        }
                        (dev as XiaomiTempSensor).SetPropFromIoBroker(zo);
                    }
                };
            client.AfterException += (sender, args) => Console.WriteLine($"AfterException: {args.Value}");
            client.Connected += (s, e) =>
            {
                Console.WriteLine("Connected");
                client.Emit("subscribe", '*');
                client.Emit("subscribeObjects", '*');
            };
            await client.ConnectAsync(new Uri("http://192.168.49.56:8084"));
        }


    }
}
