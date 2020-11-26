using AppBroker.Core.Devices;
using AppBroker.IOBroker.Data;
using AppBroker.IOBroker.Devices;

using H.Socket.IO;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppBroker.IOBroker
{
    class ZigbeeDeviceManager : IDeviceManager
    {
        private SocketIoClient client;

        private async Task ConnectToIOBroker()
        {

            client = new SocketIoClient();

            client.EventReceived += (sender, args) =>
            {
                Console.WriteLine("Received: " + args.Value);
                var suc = IoBrokerZigbee.TryParse(args.Value, out var zo);
                if (suc)
                {
                    if (Devices.TryGetValue(zo.Id, out var dev))
                        (dev as ZigbeeDevice).SetPropFromIoBroker(zo, true);
                    else
                        GetZigbeeDevices();
                }
            };
            client.ExceptionOccurred += (sender, args) => logger.Error($"AfterException: {args.Value}");
            client.Connected += (s, e) =>
            {
                Console.WriteLine("Connected");
                client.Emit("subscribe", "zigbee.*");
                client.Emit("subscribeObjects", "*");
                GetZigbeeDevices();
            };
            var random = new Random();
            await client.ConnectAsync(new Uri("http://ZigbeeHub:8084"));
        }

        public void GetZigbeeDevices()
        {

            string content = RequestStringData(@"http://ZigbeeHub:8087/objects?pattern=zigbee.0*");
            var better = Regex.Replace(content, "\"zigbee[.\\w\\s\\d]+\":", "");
            better = $"[{better[1..^1]}]";

            var ioBrokerObject = JsonConvert.DeserializeObject<ZigbeeIOBrokerProperty[]>(better);
            var idRequest = "http://ZigbeeHub:8087/get/";
            var idsAlreadyInRequest = new List<ulong>();
            var stateRequest = "http://ZigbeeHub:8087/get/";
            foreach (var item in ioBrokerObject)
            {
                var matches = item._id.Split('.');
                if (matches.Length > 2)
                {
                    if (ulong.TryParse(matches[2].ToString(), System.Globalization.NumberStyles.HexNumber, null, out var id))
                    {
                        if (!idsAlreadyInRequest.Contains(id))
                        {
                            idRequest += string.Join('.', matches.Take(3)) + ",";
                            idsAlreadyInRequest.Add(id);
                        }
                        stateRequest += item._id + ",";
                    }
                }
            }

            content = RequestStringData(idRequest);


            var getDeviceResponses = JsonConvert.DeserializeObject<IoBrokerGetDeviceResponse[]>(content);
            content = RequestStringData(stateRequest);
            var deviceStates = JsonConvert.DeserializeObject<IoBrokerStateResponse[]>(content);

            foreach (var deviceRes in getDeviceResponses)
            {
                if (!long.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
                    continue;

                if (!Devices.TryGetValue(id, out var dev))
                {
                    switch (deviceRes.common.type)
                    {
                        case "WSDCGQ11LM":
                        case "lumi.weather": dev = new XiaomiTempSensor(id); break;
                        case "lumi.router": dev = new LumiRouter(id); break;
                        case "L1529":
                        case "FLOALT panel WS 60x60": dev = new FloaltPanel(id, "http://ZigbeeHub:8087/set/" + deviceRes._id); break;
                        case "E1524/E1810":

                        case "TRADFRI remote control": dev = new TradfriRemoteControl(id); break;
                        case "AB32840":
                        case "Classic B40 TW - LIGHTIFY": dev = new OsramB40RW(id, "http://ZigbeeHub:8087/set/" + deviceRes._id); break;
                        case "AB3257001NJ":
                        case "Plug 01": dev = new OsramPlug(id, "http://ZigbeeHub:8087/set/" + deviceRes._id); break;
                        default: break;
                    }
                    if (dev == default(Device))
                        continue;
                    if (dev is ZigbeeDevice zd)
                        zd.AdapterWithId = deviceRes._id;
                    Devices.TryAdd(id, dev);
                    if (!DbProvider.AddDeviceToDb(dev))
                        DbProvider.MergeDeviceWithDbData(dev);
                    if (string.IsNullOrWhiteSpace(dev.FriendlyName))
                        dev.FriendlyName = deviceRes.common.name;
                }
                foreach (var item in deviceStates.Where(x => x._id.Contains(deviceRes.native.id)))
                {
                    var ioObject = new IoBrokerObject { ValueName = item.common.name.ToLower().Replace(" ", "_"), ValueParameter = new Parameter { Value = item.val } };
                    if (dev is XiaomiTempSensor)
                    {
                        if (ioObject.ValueName == "battery_voltage")
                            ioObject.ValueName = "voltage";
                        else if (ioObject.ValueName == "battery_percent")
                            ioObject.ValueName = "battery";
                    }
                    else if (dev is FloaltPanel)
                    {

                        if (ioObject.ValueName == "color_temperature")
                            ioObject.ValueName = "colortemp";
                        else if (ioObject.ValueName == "switch_state")
                            ioObject.ValueName = "state";
                    }

                    (dev as ZigbeeDevice).SetPropFromIoBroker(ioObject, false);
                }
            }
        }

        private string RequestStringData(string url)
        {
            var request = WebRequest.CreateHttp(url);
            var res = request.GetResponse();
            using var stream = res.GetResponseStream();
            using var streamreader = new StreamReader(stream);
            return streamreader.ReadToEnd();
        }
    }
}
