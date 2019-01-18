using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AppBokerASP.Devices;

namespace AppBokerASP
{
    public class DeviceManager
    {
        public Dictionary<uint, Device> Devices = new Dictionary<uint, Device>();

        private List<Type> types;
        public DeviceManager()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.Node.NewConnectionEstablished += Node_NewConnectionEstablished;
        }

        private void Node_NewConnectionEstablished(object sender, (PainlessMesh.Connection c, List<string> l) e)
        {
            if (!Devices.TryGetValue(e.c.NodeId, out var device))
            {
                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name == e.l[1]), e.c.NodeId);
                Console.WriteLine($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                //var h = new LedStripMesh(e.c.NodeId);
                Devices.Add(e.c.NodeId, newDevice);
            }
            else
            {
                Console.WriteLine($"Device reconnect: {device.TypeName}, {device.Id}");
                Devices.Remove(e.c.NodeId);
                Node_NewConnectionEstablished(this, e);
            }
        }
    }
}
