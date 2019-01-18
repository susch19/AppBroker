using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace PainlessMesh
{
    public class Connection : Sub
    {
        public TcpClient Client { get; set; }
        public DateTime LastReceived { get; internal set; }

        public Connection() 
        {
            Connections = new Dictionary<uint, Sub>();
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
            if (!Connections.TryGetValue(nodeId ,out var connection))
                return false;
            var con = connection as Connection;
            if (!con.Client.Connected)
            {
                RemoveConnection(connection);
                return false;
            }

            try
            {
                if(message.from == 0)
                {
                    message.from = NodeId;
                }
                var asd = JsonConvert.SerializeObject(message) + '\0';
                con.Client.Client.Send(Encoding.UTF8.GetBytes(asd));
                return true;
            }
            catch (Exception)
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
