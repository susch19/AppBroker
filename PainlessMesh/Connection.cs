using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace PainlessMesh
{
    public class Connection : Sub
    {
        //public TcpClient Client { get; set; }
        public SerialPort SerialPort { get; set; }
        public DateTime LastReceived { get; internal set; }

        public Connection()
        {
            Subs = new Dictionary<uint, Sub>();
        }

        //public Connection GetConnectionById(int id)
        //{
        //    if (NodeId == id)
        //        return this;
        //    else if (Connections.Count == 0)
        //        return null;
        //    else
        //        return Connections.FirstOrDefault(x => x.Value.ContainsId(id)).Value;
        //}

        public bool SafeWrite(uint nodeId, BasicMessage message)
        {
            if (!Subs.TryGetValue(nodeId, out var connection))
                return false;
            if (message.type == PackageType.NODE_SYNC_REPLY || message.type == PackageType.NODE_SYNC_REQUEST)
                return true;
            var con = connection as Connection;
            if (!con.SerialPort.IsOpen)
            //if (!con.Client.Connected)
            {
                RemoveConnection(connection);
                return false;
            }

            try
            {
                if (message.from == 0)
                {
                    message.from = NodeId;
                }
                var asd = JsonConvert.SerializeObject(message) + '\0';
                //con.Client.Client.Send(Encoding.UTF8.GetBytes(asd));
                con.SerialPort.WriteLine(message.dest.ToString());
                if (con.SerialPort.ReadLine() != "ACK\r")
                    return false;
                con.SerialPort.WriteLine(asd);
                if (con.SerialPort.ReadLine() == "ACK2\r")
                    return true;
                return false;

            }
            catch (Exception e)
            {

                RemoveConnection(connection);
                return false;
            }
        }
    }

    /*public class SubConnection
    {

        public int NodeId { get; internal set; }
        public Dictionary<int, SubConnection> Connections { get; set; }
        public DateTime LastReceived { get; internal set; }

    }*/
}
