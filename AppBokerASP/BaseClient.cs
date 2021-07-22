using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Authentication;
using PainlessMesh;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AppBokerASP
{
    public class BaseClient
    {
        private static X509Certificate2 ServerCert;
        private static X509Store ServerStore;

        public event EventHandler<BinarySmarthomeMessage> ReceivedData;
        public CancellationTokenSource Source;

        protected readonly TcpClient Client;
        protected readonly SslStream stream;
        private const int HeaderSize = sizeof(int);
        private CancellationToken startTaskToken;
        private byte[] headerBuffer = new byte[HeaderSize];
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
            stream = sslStream;
            try
            {
                sslStream.AuthenticateAsServer(ServerCert, true, SslProtocols.Tls12 | SslProtocols.Tls13 , false);
            }
            catch (AuthenticationException ae)
            {
                StopConnection(client, stream);
                logger.Error(ae);
            }
            catch (IOException iOException)
            {
                StopConnection(client, stream);
                logger.Error(iOException);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                StopConnection(client, stream);
                logger.Error(e);
            }
        }

        private void StopConnection(TcpClient client, SslStream sslStream)
        {
            client.Close();
            stream.Close();
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
                    int ret = stream.Read(buffer, read, count);
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
            if (stream is null)
                return;

            Span<byte> buffer = stackalloc byte[sizeof(int) + sizeof(byte) + data.Length];
            BitConverter.TryWriteBytes(buffer, nodeId);
            BitConverter.TryWriteBytes(buffer[sizeof(int)..], (byte)packageType);
            data.CopyTo(buffer[(sizeof(int) + sizeof(byte))..]);

            stream.Write(BitConverter.GetBytes(buffer.Length), 0, HeaderSize);
            stream.Write(buffer);
        }

        private static byte[] GetBytesFromPEM(string pemString, PemStringType type)
        {
            string header; string footer;
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
                    return null;
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

    }
}
