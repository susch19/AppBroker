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

namespace AppBokerASP
{
    public class BaseClient
    {
        private static X509Certificate2 ServerCert;
        private static X509Store ServerStore;

        public event EventHandler<GeneralSmarthomeMessage> ReceivedData;
        public CancellationTokenSource Source;

        protected readonly TcpClient Client;
        protected readonly SslStream stream;
        private const int HeaderSize = sizeof(int);
        private CancellationToken startTaskToken;
        private byte[] headerBuffer = new byte[HeaderSize];

        static BaseClient()
        {

            ServerStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            ServerStore.Open(OpenFlags.ReadOnly);
            var certs = ServerStore.Certificates.Find(X509FindType.FindByThumbprint, "0cd48689df687d3f95cd8e3baa3d5d772361563f", true);

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

            //SessionCert = new X509Certificate2(@"F:\painlessmeshboost\cert.pfx");

            var sslStream = new SslStream(Client.GetStream());/*, false, RemoteCertValidation, LocalCertificateSelection, EncryptionPolicy.RequireEncryption);*/
            stream = sslStream;
            try
            {
                sslStream.AuthenticateAsServer(ServerCert, true, System.Security.Authentication.SslProtocols.Tls12, false);
            }
            catch (AuthenticationException ae)
            {
                StopConnection(client, stream);
                Console.WriteLine(ae);
            }
            catch (IOException iOException)
            {
                StopConnection(client, stream);
                Console.WriteLine(iOException);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                StopConnection(client, stream);
                Console.WriteLine(e);
            }
        }

        private void StopConnection(TcpClient client, SslStream sslStream)
        {
            client.Close();
            stream.Close();
            Source.Cancel();
        }

        private X509Certificate LocalCertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return ServerCert;
        }

        private bool RemoteCertValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
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

                    int size = BitConverter.ToInt32(headerBuffer);
                    var bodyBuf = ArrayPool<byte>.Shared.Rent(size);
                    if (!ReadExactly(bodyBuf, 0, size))
                    {
                        Disconnect();
                        ArrayPool<byte>.Shared.Return(bodyBuf);
                        return;
                    }
                    var msg = Encoding.GetEncoding(437).GetString(bodyBuf, 0, size);
                    if (string.IsNullOrWhiteSpace(msg))
                        continue;
                    Console.WriteLine("Debug MSG: " + msg);
                    var o = JsonConvert.DeserializeObject<GeneralSmarthomeMessage>(msg);

                    ReceivedData?.Invoke(this, o);
                    ArrayPool<byte>.Shared.Return(bodyBuf);
                }
                Disconnect();
            }, startTaskToken);
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnect");
            Source.Cancel();
            Client.Close();
        }

        public void Send(PackageType packageType, string data, uint nodeId)
        {
            var buf = BitConverter.GetBytes(nodeId).Concat(new[] { (byte)packageType }).Concat(Encoding.GetEncoding(437).GetBytes(data)).ToArray();
            stream?.Write(BitConverter.GetBytes(buf.Length), 0, HeaderSize);
            stream?.Write(buf, 0, buf.Length);
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
