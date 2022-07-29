using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Authentication;
using PainlessMesh;
using AppBroker.Core;

namespace AppBrokerASP;

public class BaseClient : IDisposable
{
    private static readonly X509Certificate2? ServerCert;
    private static readonly X509Store ServerStore;

    public event EventHandler<BinarySmarthomeMessage>? ReceivedData;
    public CancellationTokenSource Source { get; private set; }

    protected TcpClient Client { get; }
    protected SslStream Stream { get; }
    private const int HeaderSize = sizeof(int);
    private CancellationToken startTaskToken;
    private readonly byte[] headerBuffer = new byte[HeaderSize];
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    static BaseClient()
    {

        ServerStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        ServerStore.Open(OpenFlags.ReadOnly);
        var certs = ServerStore.Certificates.Find(X509FindType.FindByThumbprint, "643b4f728a001e1b0aa429b6109ec42cb14f7881", true);

        if (certs.Count > 0)
            ServerCert = certs[0];
        else if (File.Exists("cert.pem") && File.Exists("key.pem"))
        {
            byte[] pubPem = Encoding.UTF8.GetBytes(File.ReadAllText("cert.pem").Trim());
            ServerCert = new X509Certificate2(pubPem);
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(GetBytesFromPEM(File.ReadAllText("key.pem"), PemStringType.RsaPrivateKey), out var key);
            ServerCert = new X509Certificate2(ServerCert.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx));

        }
    }

    internal BaseClient(TcpClient client)
    {
        Client = client;
        Client.NoDelay = true;
        Source = new CancellationTokenSource();

        var sslStream = new SslStream(Client.GetStream());
        Stream = sslStream;
        try
        {
            sslStream.AuthenticateAsServer(ServerCert!, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
        }
        catch (AuthenticationException ae)
        {
            StopConnection(client, Stream);

            logger.Error(ae);
        }
        catch (IOException iOException)
        {
            StopConnection(client, Stream);
            logger.Error(iOException);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            StopConnection(client, Stream);
            logger.Error(e);
        }
    }

    private void StopConnection(TcpClient client, SslStream sslStream)
    {
        client.Close();
        Stream.Close();
        Source.Cancel();
    }

    private bool ReadExactly(byte[] buffer, int offset, int count)
    {
        int read = offset;
        try
        {

            do
            {
                if (count > 10000)
                    return false;
                int ret = Stream.Read(buffer, read, count);
                if (ret <= 0)
                {
                    return false;
                }
                read += ret;
            } while (read < count);
        }
        catch (IOException ioe)
        {
            logger.Error(ioe);
            return false;
        }
        return true;
    }

    public Task Start()
    {
        startTaskToken = Source.Token;
        return Task.Run(() =>
        {
            while (!startTaskToken.IsCancellationRequested)
            {
                if (!ReadExactly(headerBuffer, 0, HeaderSize))
                {
                    Disconnect();
                    return;
                }
                try
                {

                    int size = BitConverter.ToInt32(headerBuffer);
                    if (size > 4000)
                    {
                        logger.Warn("Got very a large size for buffer " + size);
                        Disconnect();
                        return;
                    }
                    var bodyBuf = ArrayPool<byte>.Shared.Rent(size);
                    if (!ReadExactly(bodyBuf, 0, size))
                    {
                        Disconnect();
                        ArrayPool<byte>.Shared.Return(bodyBuf);
                        return;
                    }

                    using var ms = new MemoryStream(bodyBuf);

                    var nodeId = uintSerialization.Deserialize(ms);
                    var bsm = BinarySmarthomeMessageSerialization.Deserialize(ms);
                    bsm.NodeId = nodeId;

                    if (bsm.Command != Command.Mesh || bsm.NodeId != 1 || bsm.MessageType != MessageType.Update)
                        logger.Debug($"Recvd: Von: {bsm.NodeId}, Command: {bsm.Command}, MessageType: {bsm.MessageType}, ParamsAmount: {bsm.Parameters.Count}, " + string.Join(", ", bsm.Parameters.Select(x => BitConverter.ToString(x))));
                        //var msg = Encoding.GetEncoding(437).GetString(bodyBuf, 0, size);
                        //if (string.IsNullOrWhiteSpace("Msg: " + msg))
                        //    continue;

                        //logger.Debug(msg);
                        //var o = JsonConvert.DeserializeObject<BinarySmarthomeMessage>(msg);

                        ReceivedData?.Invoke(this, bsm);
                    ArrayPool<byte>.Shared.Return(bodyBuf);
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
            Disconnect();
        }, startTaskToken);
    }

    public void Disconnect()
    {
        logger.Debug("Disconnect");
        Source.Cancel();
        Client.Close();
    }

    public void Send(PackageType packageType, Span<byte> data, uint nodeId)
    {
        if (Stream is null)
            return;

        Span<byte> buffer = stackalloc byte[sizeof(int) + sizeof(byte) + data.Length];
        _ = BitConverter.TryWriteBytes(buffer, nodeId);
        buffer[sizeof(int)] =  (byte)packageType;
        data.CopyTo(buffer[(sizeof(int) + sizeof(byte))..]);

        Stream.Write(BitConverter.GetBytes(buffer.Length), 0, HeaderSize);
        Stream.Write(buffer);
    }

    private static byte[] GetBytesFromPEM(string pemString, PemStringType type)
    {
        string header;
        string footer;
        switch (type)
        {
            case PemStringType.Certificate:
                header = "-----BEGIN CERTIFICATE-----";
                footer = "-----END CERTIFICATE-----";
                break;
            case PemStringType.RsaPrivateKey:
                header = "-----BEGIN PRIVATE KEY-----";
                footer = "-----END PRIVATE KEY-----";
                break;
            default:
                return Array.Empty<byte>();
        }

        int start = pemString.IndexOf(header) + header.Length;
        int end = pemString.IndexOf(footer, start) - start;
        return Convert.FromBase64String(pemString.Substring(start, end));
    }
    private enum PemStringType
    {
        Certificate,
        RsaPrivateKey
    }

    public void Dispose()
    {
        Source?.Dispose();
        Stream?.Dispose();
    }
}
