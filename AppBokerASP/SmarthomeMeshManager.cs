using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PainlessMesh;

namespace AppBokerASP
{
    public class SmarthomeMeshManager
    {
        public Node Node { get; private set; }
        private string connectionUrl => GetGatewayAddress();

        private readonly object locking = new object();
        private bool changed = false;

        private Task runningTask;

        public SmarthomeMeshManager() => Node = new Node(120);


        private string GetGatewayAddress()
        {

            var ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name == "wlan0");

            if (ni != null)
            {
                return ni.GetIPProperties()?.GatewayAddresses?.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString();
            }
            else
            {
                return "10.9.254.1";//10.104.130.1, 10.12.206.1, 10.124.187.1
            }
        }

        private void PrintToConsole(string s)
        {
            Console.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ": " + s);

        }

        public void Start()
        {
            NetworkChange.NetworkAddressChanged += (s, e) =>
            {
                changed = true;
            };

            runningTask = new Task(async () =>
            {
                while (true)
                {
                    if (!await DoMoreStuffAsync())
                    {
                        changed = true;
                        PrintToConsole("Connection aborted");
                    }
                    Thread.Sleep(2000);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning);
            runningTask.Start();
        }

        private async Task<bool> DoMoreStuffAsync()
        {
            while (true)
            {
                //if (Node.Client == null || Node.Client.Connected == false || changed)
                if (Node.SerialPort == null || Node.SerialPort.IsOpen == false || changed)
                {
                    changed = false;

                    //string connectionPort = "COM15";
                    string connectionPort = "COM256";
                    int connectionSpeed = 512000;
                    //int connectionSpeed = 2000000;
                    //int connectionSpeed = 115200;
                    try
                    {
                        //PrintToConsole("Try Connect: " + connectionUrl);
                        //await Node.ConnectTCPAsync(connectionUrl, 5555);
                        PrintToConsole("Try Connect: " + connectionPort + " at " + connectionSpeed);
                        Node.ConnectSerial(connectionPort, connectionSpeed);
                    }
                    catch (Exception ex)
                    {
                        PrintToConsole(ex.Message);
                        return false;
                    }
                    PrintToConsole("Connect sucessful: " + connectionPort + " at " + connectionSpeed);

                    //PrintToConsole("Connect sucessful: " + connectionUrl);
                }
                Thread.Sleep(250);
            }
        }

        //while (true)
        //{
        //    try
        //    {

        //        await Node.ConnectTCPAsync(connectionUrl, 5555);
        //        //await Node.ConnectTCPAsync("10.9.254.1", 5555);
        //        Console.WriteLine("Connection sucessful: " + connectionUrl);
        //        break;
        //    }
        //    catch (Exception)
        //    {
        //        Console.WriteLine("Connection failed: " + connectionUrl);
        //        Thread.Sleep(250);
        //    }
        //}

    }
}

