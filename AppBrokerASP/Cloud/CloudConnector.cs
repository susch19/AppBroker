using AppBroker.Core;

using AppBrokerASP.Configuration;

using NLog;

using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace AppBrokerASP.Cloud;

public class CloudConnector
{
    ConcurrentDictionary<TcpClient, TcpClient> tcpClients = new();
    private readonly Logger mainLogger;
    private readonly CloudConfig config;


    public CloudConnector()
    {
        mainLogger = LogManager.GetCurrentClassLogger();
        config = InstanceContainer.Instance.ConfigManager.CloudConfig;
        if (config.Enabled)
        {
            try
            {
                var client = new TcpClient();
                mainLogger.Warn("Waiting for TCP Connection");
                client.BeginConnect(config.CloudServerHost, config.CloudServerPort, OnClientConnect, client);
            }
            catch (Exception ex)
            {
                mainLogger.Error("No connection to the cloud server could be established", ex);
            }
        }
    }

    void OnClientConnect(IAsyncResult x)
    {
        var client = (TcpClient)x.AsyncState!;
        try
        {
            client.EndConnect(x);
            mainLogger.Warn("Busy Waiting for new Connection >>>>>>>>>>>>>");
            AcceptNewClient(client);
        }
        catch (Exception)
        {
            try
            {
                client.BeginConnect(config.CloudServerHost, config.CloudServerPort, OnClientConnect, client);

            }
            catch (Exception ex)
            {
                mainLogger.Error("No connection to the cloud server could be established", ex);
            }
            return;
        }
        client = new TcpClient();
        mainLogger.Warn("Waiting for TCP Connection >>>>>>>>>>>>> ");
        client.BeginConnect(config.CloudServerHost, config.CloudServerPort, OnClientConnect, client);
    }

    private void AcceptNewClient(TcpClient x)
    {
        mainLogger.Warn("Starting new connection >>>>>>>>>>>>> ");
        var incomming = x;

        Stream incommingStr = incomming.GetStream();
        //var str = """
        //GET /SmartHome/YourUniqueConnectionId/Server undefined
        //Host: smarthome.susch.eu
        //User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:102.0) Gecko/20100101 Firefox/102.0
        //Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8
        //Accept-Language: de,en-US;q=0.7,en;q=0.3
        //Accept-Encoding: gzip, deflate, br
        //Connection: keep-alive
        //Upgrade-Insecure-Requests: 1
        //Sec-Fetch-Dest: document
        //Sec-Fetch-Mode: navigate
        //Sec-Fetch-Site: none
        //Sec-Fetch-User: ?1
        //""";

        //incommingStr.Write(Encoding.UTF8.GetBytes(str));
        //Thread.Sleep(250);
        //Span<byte> msg = stackalloc byte[incomming.Available];
        //incommingStr.Read(msg);
        //var msgStr = Encoding.UTF8.GetString(msg);
        //Console.Write(msgStr);

        if (config.UseSSL)
        {

            incommingStr = new SslStream(incommingStr);
            var ssl = incommingStr as SslStream;
            ssl.AuthenticateAsClient(config.CloudServerHost);

        }
        /*
GET / HTTP/1.1
Host: smarthome.susch.eu
Connection: keep-alive
         */

        //incommingStr.Write(Encoding.UTF8.GetBytes($"GET /SmartHome/{config.ConnectionID}/Server HTTP/1.1\r\nHost: smarthome.susch.eu\r\nConnection: keep-alive\r\n"));
        incommingStr.Write(Encoding.UTF8.GetBytes($"/SmartHome/{config.ConnectionID}/Server"));
        //Thread.Sleep(250);
        //Span<byte> msg = stackalloc byte[incomming.Available];
        //incommingStr.Read(msg);
        //var msgStr = Encoding.UTF8.GetString(msg);
        //Console.Write(msgStr);

        Span<byte> firstMessage = stackalloc byte[1];

        do
        {
            incommingStr.ReadExactly(firstMessage);
        } while (firstMessage[0] != 1);


        var self = new TcpClient(config.LocalHostName, Program.UsedPortForSignalR);
        self.NoDelay = true;
        var selfStr = self.GetStream();
        tcpClients[incomming] = self;

        _ = Task.Run(async () =>
        {
            mainLogger.Warn("Server started >>>>>>>>>>>>> ");
            while (true)
            {
                try
                {
                    if (self.Available > 0)
                    {
                        var bytes = new byte[self.Available];
                        selfStr.ReadExactly(bytes);
                        mainLogger.Warn($"Send back {bytes.Length}");
                        var bp = System.Text.Encoding.UTF8.GetString(bytes);
                        incommingStr.Write(bytes);
                    }
                }
                catch (Exception ex)
                {
                    mainLogger.Warn($"Send error (Client:{incomming.Connected}, Self:{self.Connected}) {ex}");
                    tcpClients.Remove(incomming, out _);
                    incomming?.Close();
                    self?.Close();
                    break;

                }
                await Task.Delay(1);
            }
        });
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (incomming.Available > 0)
                    {
                        var bytes = new byte[incomming.Available];
                        incommingStr.ReadExactly(bytes);
                        var bp = System.Text.Encoding.UTF8.GetString(bytes);
                        mainLogger.Warn($"Rec {bytes.Length}");
                        selfStr.Write(bytes);

                    }
                }
                catch (Exception ex)
                {
                    mainLogger.Warn($"Rec error (Client:{incomming.Connected}, Self:{self.Connected}) {ex}");

                    tcpClients.Remove(incomming, out _);
                    incomming?.Close();
                    self?.Close();
                    break;
                }
                await Task.Delay(1);
            }
        });
    }
}
