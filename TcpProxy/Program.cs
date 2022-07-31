using NLog;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

using TcpProxy;

Logger mainLogger = LogManager
    .Setup()
    .GetCurrentClassLogger()!;
mainLogger.Info("Starting TCP Proxy");
var v6Listener = new TcpListener(IPAddress.IPv6Any, 5057);
v6Listener.Server.DualMode = true;

v6Listener.Start();
mainLogger.Info("Listening on Port 5057");

ConcurrentDictionary<string, List<(TcpClient client, NetworkStream str)>> servers = new();
ConcurrentDictionary<string, List<(TcpClient client, NetworkStream str)>> clients = new();

ConcurrentDictionary<TcpClient, (TcpClient client, NetworkStream str)> clientToServer = new();
ConcurrentDictionary<TcpClient, (TcpClient client, NetworkStream str)> serverToClient = new();

SemaphoreSlim connectionSemaphore = new SemaphoreSlim(1);

Task.Run(PingServers);

int clientsAccepted = 0;
v6Listener.BeginAcceptTcpClient(OnServerConnected, v6Listener);

var waitForProcessShutdownStart = new ManualResetEvent(false);
using var reg = PosixSignalRegistration.Create(OperatingSystem.IsWindows() ? PosixSignal.SIGINT : PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;

    waitForProcessShutdownStart.Set();
});
waitForProcessShutdownStart.WaitOne();

async Task AddNewTcpClient(TcpClient self)
{
    bool alreadyReadFirstMessage = false;
    bool isServer = false;
    var selfStr = self.GetStream();
    self.NoDelay = true;
    string id = "";
    DateTime lastReceived = DateTime.UtcNow;
    while (true)
    {
        if (!self.Connected || (!isServer && lastReceived.AddMinutes(5) < DateTime.UtcNow))
        {
            (TcpClient, NetworkStream) server, client;

            if (isServer)
            {
                server = (self, selfStr);
                client = serverToClient[self];
            }
            else
            {
                client = (self, selfStr);
                server = clientToServer[self];
            }

            connectionSemaphore.Wait();
            try
            {
                if (!clients.TryGetValue(id, out var list))
                    _ = list?.Remove(client);
                if (!servers.TryGetValue(id, out var list2))
                    _ = list2?.Remove(server);
            }
            finally
            {
                _ = connectionSemaphore.Release();
            }
            _ = serverToClient.Remove(server.Item1, out _);
            _ = clientToServer.Remove(client.Item1, out _);
            WriteLog($"Disconnect {(isServer ? "Server" : "Client")}", ConsoleColor.Red);
            break;
        }

        if (!alreadyReadFirstMessage && self.Available == 0)
            WriteLog("Handshake not completed yet");
        byte[] bytes = null;
        if (self.Available > 0)
        {
            lastReceived = DateTime.UtcNow;
            bytes = new byte[self.Available];
            selfStr.ReadExactly(bytes);
            bool wasFirstMessage = false;
            if (!alreadyReadFirstMessage)
            {
                alreadyReadFirstMessage = true;
                wasFirstMessage = true;

                var registerCall = Encoding.UTF8.GetString(bytes);
                //Console.WriteLine(registerCall);

                var pathValues = registerCall.Split('/');

                id = pathValues[2].Split('?').First();

                if (registerCall.Contains("Server"))
                {
                    connectionSemaphore.Wait();
                    try
                    {
                        if (!servers.TryGetValue(id, out var list))
                            servers[id] = list = new();
                        list.Add((self, selfStr));
                    }
                    finally
                    {
                        _ = connectionSemaphore.Release();
                    }
                    isServer = true;
                    WriteLog($"New Server for {id} {self.GetHashCode():x}", ConsoleColor.Green);

                    if (clients.TryGetValue(id, out var clientList) && !serverToClient.ContainsKey(self))
                    {
                        foreach (var client in clientList)
                        {
                            if (client.client.Connected && !clientToServer.ContainsKey(client.client) && !serverToClient.ContainsKey(self))
                            {

                                connectionSemaphore.Wait();
                                try
                                {
                                    serverToClient[self] = client;
                                    clientToServer[client.client] = (self, selfStr);
                                    //Console.WriteLine($"Starting Server {self.GetHashCode():x2}");
                                    selfStr.WriteByte(1);
                                }
                                finally
                                {
                                    _ = connectionSemaphore.Release();
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    connectionSemaphore.Wait();
                    try
                    {
                        if (!clients.TryGetValue(id, out var list))
                            clients[id] = list = new();
                        list.Add((self, selfStr));

                    }
                    finally
                    {
                        _ = connectionSemaphore.Release();
                    }
                    WriteLog($"New Client for {id} {self.GetHashCode():x}", ConsoleColor.Green);


                }
            }
            if (isServer && !wasFirstMessage)
            {
                if (serverToClient.TryGetValue(self, out var connection))
                {
                    try
                    {
                        connection.str.Write(bytes);
                        WriteLog($"Forwarding {bytes.Length} to client {id}", ConsoleColor.Gray);

                    }
                    catch (Exception ex)
                    {
                        connection.client.Close();
                        RemoveServer(self, id, connection);
                        WriteLog($"Close client {id}: {ex}", ConsoleColor.Red);
                    }
                }
                else
                {

                    self.Close();
                    RemoveServer(self, id, connection);
                    WriteLog("Havent gotten any client to send message to :( " + self.GetHashCode(), ConsoleColor.Yellow);
                }
            }
            else
            {

                if (clientToServer.TryGetValue(self, out var connection))
                {
                    try
                    {
                        connection.str.Write(bytes);
                        WriteLog($"Forwarding {bytes.Length} to server {id}", ConsoleColor.Gray);

                    }
                    catch (Exception ex)
                    {
                        connection.client.Close();
                        RemoveClient(self, id, connection);
                        WriteLog($"Close server {id}: {ex}", ConsoleColor.Red);
                    }
                }
                else
                    WriteLog("Havent gotten any server to send message to :( " + self.GetHashCode(), ConsoleColor.Yellow);

            }

        }
        else
        {
            await Task.Delay(1);
        }

        if (!isServer && !clientToServer.ContainsKey(self) && servers.TryGetValue(id, out var serverList))
        {

            bool hasConnection = clientToServer.ContainsKey(self);
            //while (!hasConnection)
            {

                foreach (var server in serverList)
                {
                    if (server.client.Connected && !serverToClient.ContainsKey(server.client))
                    {

                        connectionSemaphore.Wait();
                        try
                        {
                            clientToServer[self] = server;
                            serverToClient[server.client] = (self, selfStr);

                            Console.WriteLine("Starting server connection");
                            server.str.WriteByte(1);
                            if (bytes is not null)
                                server.str.Write(bytes);
                            hasConnection = true;
                        }
                        finally
                        {

                            _ = connectionSemaphore.Release();
                        }
                        break;
                    }
                }
                WriteLog("Server missing");
                await Task.Delay(100);

            }
        }

    }

}

void OnServerConnected(IAsyncResult ar)
{
    TcpListener? listener = (TcpListener)ar.AsyncState;
    var client = listener.EndAcceptTcpClient(ar);
    WriteLog($"Accepted new Client {clientsAccepted++}: {client.GetHashCode()}", ConsoleColor.Green);
    _ = AddNewTcpClient(client);
    _ = listener.BeginAcceptTcpClient(OnServerConnected, listener);
    WriteLog($"Ready for new Client {clientsAccepted}", ConsoleColor.Green);
}



void WriteLog(string message, ConsoleColor cc = ConsoleColor.White)
{
    var before = Console.ForegroundColor;
    Console.ForegroundColor = cc;
    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}: {message}");
    Console.ForegroundColor = before;
}

void RemoveServer(TcpClient self, string id, (TcpClient client, NetworkStream str) connection)
{
    try
    {
        if (!clients.TryGetValue(id, out var list))
            clients[id] = list = new();
        _ = list.Remove(connection);
        _ = serverToClient.Remove(self, out _);
        _ = clientToServer.Remove(connection.client, out _);
    }
    finally
    {
        _ = connectionSemaphore.Release();
    }
}

void RemoveClient(TcpClient self,
    string id,
    (TcpClient client, NetworkStream str) connection)
{
    try
    {
        if (!servers.TryGetValue(id, out var list))
            clients[id] = list = new();
        _ = list.Remove(connection);
        _ = clientToServer.Remove(self, out _);
        _ = serverToClient.Remove(connection.client, out _);
    }
    finally
    {
        _ = connectionSemaphore.Release();
    }
}

void PingServers()
{
    while (true)
    {

        foreach (var (_, item) in servers)
        {
            connectionSemaphore.Wait();
            try
            {
                for (int i = item.Count - 1; i >= 0; i--)
                {
                    (TcpClient client, NetworkStream str) = item[i];
                    if (!serverToClient.ContainsKey(client))
                    {
                        try
                        {
                            str.WriteByte(123);
                        }
                        catch
                        {
                            item.RemoveAt(i);
                        }
                    }
                }
            }
            finally
            {
                connectionSemaphore.Release();
            }
        }
        Thread.Sleep(10000);
    }
}