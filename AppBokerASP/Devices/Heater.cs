using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PainlessMesh;

namespace AppBokerASP.Devices
{
    public class Heater : Device
    {
        private readonly Timer timer;
        private readonly Random r = new Random();
        private readonly Connection connection;
        //private byte[] dataToSend;
        

        private async void SendUpdatedData(string data)
        {
            PrintableInformation[0] = data;
            SmartHome.ConnectedClients.ForEach(async x => await x.SendAsync("Update", this));
        }

        public Heater(uint id)
        {
            Id = id;
            TypeName = GetType().Name;
            PrintableInformation.Add("Something");
            Program.Node.SingleMessageReceived += Node_SingleMessageReceived;
            timer = new Timer(timerCallback, null, 0, 10000);
        }

        private void timerCallback(object state) {
            Program.Node.SendSingle(Id, JsonConvert.SerializeObject(new GeneralSmarthomeMessage() { MessageType = "Get", Command = "IP"}));
        }

        private void Node_SingleMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            //COMMUNICATION: In sendMessage(destId): destId = 120 type = 9, msg ={ "MessageType":"Set", "Command":"IPSet", "Parameters":["10.9.254.2","10.12.206.1", "Heater"]
    
            if (e.id != Id)
                return;
            SendUpdatedData(string.Join('\n', e.Parameters));
        }

        public override async void UpdateFromApp(string command, List<string> parameter)
        {
            if (command == "SetTemp")
            {
                Rootobject ro = new Rootobject();
                ro.MessageType = "Update";
                ro.Parameters = parameter;
                ro.Command = command;
                Program.Node.SendSingle(Id, JsonConvert.SerializeObject(ro));
            }
        }

        public void Update(string message)
        {

        }

        //public async Task Update(HttpContext context, WebSocket webSocket)
        //{
        //    var buffer = new byte[1024 * 4];
        //    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
        //    CancellationToken.None);
        //    while (!result.CloseStatus.HasValue)
        //    {
        //        if (dataToSend != null)
        //        {
        //            await webSocket.SendAsync(new ArraySegment<byte>(dataToSend, 0, dataToSend.Length), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
        //            dataToSend = null;
        //        }

        //        if (result.MessageType == WebSocketMessageType.Text)
        //        {
        //            SendUpdatedData(Encoding.ASCII.GetString(buffer));
        //        }
        //        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    }
        //    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //}
    }
}
