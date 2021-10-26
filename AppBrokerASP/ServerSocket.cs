using PainlessMesh;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AppBrokerASP
{
    public class ServerSocket
    {
        public event EventHandler<BaseClient>? OnClientConnected;

        private TcpListener? tcpListener;
        private readonly ConcurrentDictionary<BaseClient, byte> clients;
        private readonly Task sendTask;
        private readonly ConcurrentQueue<SendMessageForQueue> sendQueue;
        private readonly List<BaseClient> clientsToRemove = new();
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        public ServerSocket()
        {
            clients = new ConcurrentDictionary<BaseClient, byte>();
            sendQueue = new ConcurrentQueue<SendMessageForQueue>();
            sendTask = Task.Run(SendMessagesFromQueue);
        }

        public void Start(IPAddress address, int port)
        {
            tcpListener = new TcpListener(address, port);
            tcpListener.Start();
            _ = tcpListener.BeginAcceptTcpClient(OnClientAccepted, null);
        }

        public void Stop()
        {
            foreach (var item in clients.Keys)
                item.Disconnect();
            clients.Clear();

            tcpListener?.Stop();
        }


        private void OnClientAccepted(IAsyncResult ar)
        {
            var tmpListen = tcpListener!.EndAcceptTcpClient(ar);
            var tmpClient = new BaseClient(tmpListen);

            clients.TryAdd(tmpClient, 0);
            OnClientConnected?.Invoke(this, tmpClient);

            _ = tmpClient.Start();

            _ = tcpListener.BeginAcceptTcpClient(OnClientAccepted, null);
        }

        public void SendToAllClients(PackageType packageType, Memory<byte> data, uint nodeId, bool logMessage = true)
            => sendQueue.Enqueue(new SendMessageForQueue { PackageType = packageType, Data = data, NodeId = nodeId, LogMessage = logMessage });

        public void SendToAllClients(PackageType packageType, Memory<byte> data, bool logMessage = true)
            => sendQueue.Enqueue(new SendMessageForQueue { PackageType = packageType, Data = data, NodeId = 0, LogMessage = logMessage });

        private void SendMessagesFromQueue()
        {
            while (true)
            {
                DequeAndSend();
            }

            void DequeAndSend()
            {
                if (sendQueue.TryDequeue(out var msg))
                {
                    foreach (var keyClient in clients)
                    {
                        var client = keyClient.Key;
                        try
                        {
                            if (client.Source.IsCancellationRequested)
                            {
                                clientsToRemove.Add(client);
                                continue;
                            }
                            //if (msg.LogMessage)
                            //    logger.Debug($"Send to NodeId: {msg.NodeId}, Type: {msg.PackageType}, Data: {msg.Data}");
                            client.Send(msg.PackageType, msg.Data.Span, msg.NodeId);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            logger.Error(e);
                            clientsToRemove.Add(client);
                        }
                    }
                    if (clientsToRemove.Count > 0)
                    {
                        foreach (var item in clientsToRemove)
                        {
                            clients.Remove(item, out _);
                        }
                        clientsToRemove.Clear();
                    }
                    Thread.Sleep(100);
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
            public Memory<byte> Data { get; set; }
            public uint NodeId { get; set; }
            public bool LogMessage { get; internal set; }
        }
    }
}
