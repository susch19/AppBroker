
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppBroker.PainlessMesh;
//internal class SerialClient
//{
//    public System.IO.Ports.SerialPort SerialPort { get; set; }

//    private CancellationTokenSource cts = new CancellationTokenSource();
//    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

//    private const int HeaderSize = sizeof(int) + sizeof(int) + sizeof(byte);

//    public SerialClient()
//    {
//        SerialPort = new SerialPort("/dev/ttyUSB1", 256000);
//        SerialPort.Open();
//        _ = Task.Run(() => Read(cts.Token), cts.Token);
//        this.logger = logger;
//    }

//    public void Read(CancellationToken token)
//    {
//        while (!token.IsCancellationRequested)
//        {
//            try
//            {

//            }
//            catch (TimeoutException) { }
//        }
//    }

//    private bool ReadExactly(byte[] buffer, int offset, int count)
//    {
//        int read = offset;
//        try
//        {

//            do
//            {
//                if (count > 10000)
//                    return false;
//                int ret = SerialPort.Read(buffer, read, count);
//                if (ret <= 0)
//                {
//                    return false;
//                }
//                read += ret;
//            } while (read < count);
//        }
//        catch (IOException ioe)
//        {
//            logger.Error(ioe);
//            return false;
//        }
//        return true;
//    }

//    public void Send(PackageType packageType, Span<byte> data, uint nodeId)
//    {
//        if (!SerialPort.IsOpen)
//            return;

//        Span<byte> buffer = stackalloc byte[HeaderSize + data.Length];
//        _ = BitConverter.TryWriteBytes(buffer, buffer.Length);
//        _ = BitConverter.TryWriteBytes(buffer[sizeof(int)..], nodeId);
//        buffer[sizeof(int) + sizeof(int)] = (byte)packageType;
//        data.CopyTo(buffer[(sizeof(int) + sizeof(int) + sizeof(byte))..]);

//        SerialPort.Write(BitConverter.GetBytes(buffer.Length), 0, HeaderSize);
//        SerialPort.BaseStream.Write(buffer);
//    }
//}
