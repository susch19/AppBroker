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
            var client = new TcpClient();
            mainLogger.Warn("Waiting for TCP Connection");
            client.BeginConnect(config.CloudServerHost, config.CloudServerPort, OnClientConnect, client);
        }
    }

    void OnClientConnect(IAsyncResult x)
    {
        var client = (TcpClient)x.AsyncState!;
        client.EndConnect(x);
        mainLogger.Warn("Busy Waiting for new Connection >>>>>>>>>>>>>");
        AcceptNewClient(client);
        client = new TcpClient();
        mainLogger.Warn("Waiting for TCP Connection >>>>>>>>>>>>> ");
        client.BeginConnect(config.CloudServerHost, config.CloudServerPort, OnClientConnect, client);
    }

    private void AcceptNewClient(TcpClient x)
    {
        mainLogger.Warn("Starting new connection >>>>>>>>>>>>> ");
        var incomming = x;

        Stream incommingStr = incomming.GetStream();

        if (config.UseSSL)
        {

            incommingStr = new SslStream(incommingStr);
            var ssl = incommingStr as SslStream;
            ssl.AuthenticateAsClient(config.CloudServerHost);

        }

        incommingStr.Write(Encoding.UTF8.GetBytes($"GET /SmartHome/{config.ConnectionID}/Server"));
        //Thread.Sleep(250);
        //incommingStr.Read(msg);
        //var msgStr = Encoding.UTF8.GetString(msg);
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
