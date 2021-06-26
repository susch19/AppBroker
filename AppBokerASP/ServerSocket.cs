using PainlessMesh;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppBokerASP
{
    public class ServerSocket
    {
        public event EventHandler<BaseClient> OnClientConnected;

        private TcpListener tcpListener;
        private readonly ConcurrentBag<BaseClient> clients;
        private readonly Task sendTask;
        private readonly ConcurrentQueue<SendMessageForQueue> sendQueue;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ServerSocket()
        {
            clients = new ConcurrentBag<BaseClient>();
            sendQueue = new ConcurrentQueue<SendMessageForQueue>();
            sendTask = Task.Run(SendMessagesFromQueue);
        }

        public void Start(IPAddress address, int port)
        {
            tcpListener = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), port);
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
            foreach (var item in clients)
                item.Disconnect();
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

        public void SendToAllClients(PackageType packageType, string data, long nodeId)
            => sendQueue.Enqueue(new SendMessageForQueue { PackageType = packageType, Data = data, NodeId = nodeId });

        public void SendToAllClients(PackageType packageType, string data)
            => sendQueue.Enqueue(new SendMessageForQueue { PackageType = packageType, Data = data, NodeId = 0 });

        private void SendMessagesFromQueue()
        {
            while (true)
            {
                if (sendQueue.TryDequeue(out var msg))
                {
                    foreach (var client in clients)
                    {
                        try
                        {
                            if (client.Source.IsCancellationRequested)
                            {
                                clients.TryTake(out var a);
                                continue;
                            }
                            logger.Debug($"Send to NodeId: {msg.NodeId}, Type: {msg.PackageType}, Data: {msg.Data}");
                            client.Send(msg.PackageType, msg.Data, msg.NodeId);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            logger.Error(e);
                            clients.TryTake(out var a);
                        }
                    }
                    Thread.Sleep(250);
                }
                else
                {
                    Thread.Sleep(16);
                }
            }
        }

        private class SendMessageForQueue
        {
            public PackageType PackageType { get; set; }
            public string Data { get; set; }
            public long NodeId { get; set; }
        }
    }
}
