using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Devices;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;

namespace AppBokerASP
{
    public class DeviceManager
    {
        public Dictionary<uint, Device> Devices = new Dictionary<uint, Device>();

        private List<Type> types;
        public DeviceManager()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
        }


        private void DeviceReconnect((Sub c, List<string> l) e, List<Subscriber> clients)
        {
            var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
            newDevice.Subscribers = clients;
            Console.WriteLine($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
            Devices.Add(e.c.NodeId, newDevice);
        }

        private void Node_NewConnectionEstablished(object sender, (Sub c, List<string> l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                Devices.Remove(e.c.NodeId);
                device.StopDevice();
                DeviceReconnect(e, device.Subscribers);
                device = null;
            }
            else
            {
                using var cont = DbProvider.BrokerDbContext;

                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
                Console.WriteLine($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                Devices.Add(e.c.NodeId, newDevice);
                if (!cont.Devices.Any(x => x.Id == e.c.NodeId))
                {
                    cont.Add(newDevice.GetModel());
                    cont.SaveChanges();
                }
                //using var cont = DbProvider.BrokerDbContext;
                //if(!cont.Devices.Any(x => x.Id == e.c.NodeId))
                //{
                //    cont.Devices.Add(newDevice);
                //    cont.SaveChanges();
                //}
            }
        }
    }
}
