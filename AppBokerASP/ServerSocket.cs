using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AppBokerASP
{

    public class ServerSocket
    {

        public event EventHandler<BaseClient> OnClientConnected;

        private TcpListener tcpListener;
        private readonly List<BaseClient> clients;

        public ServerSocket()
        {
            clients = new List<BaseClient>();
        }

        public void Start(IPAddress address, int port)
        {
            tcpListener = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), 8801);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(OnClientAccepted, null);
        }
        public void Start(string host, int port)
        {
            var address = Dns.GetHostAddresses(host).FirstOrDefault(
                a => a.AddressFamily == tcpListener.Server.AddressFamily);

            Start(address, port);
        }

        public void Stop()
        {
            clients.ForEach(x => x.Disconnect());
            clients.Clear();

            tcpListener.Stop();
        }


        private void OnClientAccepted(IAsyncResult ar)
        {
            var tmpListen = tcpListener.EndAcceptTcpClient(ar);
            var tmpClient = new BaseClient(tmpListen);
            clients.Add(tmpClient);
            OnClientConnected?.Invoke(this, tmpClient);

            tmpClient.Start();

            tcpListener.BeginAcceptTcpClient(OnClientAccepted, null);
        }

        public void SendToAllClients(string data)
        {
            foreach (var client in clients)
            {
                try
                {
                    client.Send(data);
                }
                catch (Exception)
                {
                    clients.Remove(client);
                }
            }

        }
    }
}
