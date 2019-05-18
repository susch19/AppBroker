using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AppBokerASP.Devices;
using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP
{
    public class DeviceManager
    {
        public Dictionary<uint, Device> Devices = new Dictionary<uint, Device>();

        private List<Type> types;
        public DeviceManager()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.MeshManager.Node.NewConnectionEstablished += Node_NewConnectionEstablished;
        }


        private void DeviceReconnect((PainlessMesh.Connection c, List<string> l) e, List<Subscriber> clients)
        {
            var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name == e.l[1]), e.c.NodeId);
            newDevice.Subscribers = clients;
            Console.WriteLine($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
            Devices.Add(e.c.NodeId, newDevice);
        }

        private void Node_NewConnectionEstablished(object sender, (PainlessMesh.Connection c, List<string> l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                Devices.Remove(e.c.NodeId);
                DeviceReconnect(e, device.Subscribers);
            }
            else
            {
                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name == e.l[1]), e.c.NodeId);
                Console.WriteLine($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                Devices.Add(e.c.NodeId, newDevice);
            }
        }
    }
}
