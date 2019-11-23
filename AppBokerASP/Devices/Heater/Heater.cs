using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;
using Newtonsoft.Json;
using AppBokerASP.Database;
using AppBokerASP.Database.Model;

namespace AppBokerASP.Devices.Heater
{
    public class Heater : Device
    {
        //private readonly Timer timer;
        private readonly Random r = new Random();
        private readonly Connection connection;
        private readonly List<HeaterConfig> timeTemps = new List<HeaterConfig>();
        //private byte[] dataToSend;


        private void SendUpdatedData(string data)
        {
            PrintableInformation[0] = data;
            Subscribers.ForEach(async x => await x.ClientProxy.SendAsync("Update", this));
        }

        public Heater() : base(0)
        {
        }

        public Heater(uint id) : base(id)
        {
            TypeName = GetType().Name;
            PrintableInformation.Add("Something");
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            using var cont = DbProvider.BrokerDbContext;
            timeTemps.AddRange(cont.HeaterConfigs.Where(x => x.Device.Id == id).ToList().Select(x=>(HeaterConfig)x));
        }


        private void Node_SingleUpdateMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            switch (e.Command)
            {
                case Command.Temp:
                    Console.WriteLine(e.Parameters[0].GetSingle());
                    break;
                default:
                    break;
            }
            //if (e.Command ==  "Update")
            //    SendUpdatedData(string.Join('\n', e.Parameters));
            
        }

        public override void UpdateFromApp(Command command, List<JsonElement> parameter)
        {

            if (command == Command.Temp)
            {
                var msg = new GeneralSmarthomeMessage(Id, MessageType.Update, command, parameter?.ToArray());
                Program.MeshManager.SendSingle(Id, msg);
            }
        }
        public override void OptionsFromApp(Command command, List<JsonElement> parameter)
        {

            if (command == Command.Temp)
            {
                var hc = parameter.Select(x => x.GetString().ToObject<HeaterConfig>());

                UpdateDB(hc);

                var ttm = hc.Select(x => new TimeTempMessageLE(x.DayOfWeek, new TimeSpan(x.TimeOfDay.Hour, x.TimeOfDay.Minute, 0), (float)x.Temperature));
                var s = "";
                foreach (var item in ttm)
                    s += item.ToString();

                var msg = new GeneralSmarthomeMessage(Id, MessageType.Options, command, s.ToJsonElement());
                Program.MeshManager.SendSingle(Id, msg);
            }
        }

        private void UpdateDB(IEnumerable<HeaterConfig> hc)
        {
            timeTemps.Clear();
            timeTemps.AddRange(hc);

            var models = hc.Select(x => (HeaterConfigModel)x).ToList();
            using var cont = DbProvider.BrokerDbContext;
            var d= cont.Devices.FirstOrDefault(x => x.Id == Id);
            foreach (var item in models)
                item.Device = d;

            var oldConfs = cont.HeaterConfigs.Where(x => x.Device.Id == Id);
            cont.RemoveRange(oldConfs);
            cont.SaveChanges();
            cont.AddRange(models);
            cont.SaveChanges();
        }

        public void Update(string message)
        {

        }

        public override void StopDevice()
        {
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
        }

        public override dynamic GetConfig() => timeTemps.ToJson();

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
