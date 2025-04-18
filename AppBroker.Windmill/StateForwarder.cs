using AppBroker.Core;
using AppBroker.Windmill.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Windmill
{
    internal class StateForwarder
    {
        private WindmillConfig? config;
        private readonly HttpClient client;

        public StateForwarder(HttpClient client)
        {
            this.client = client;
        }

        public async Task Forward(StateChangeArgs stateChange)
        {
            if (stateChange.OldValue == stateChange.NewValue)
                return;
            config ??= IInstanceContainer.Instance.ConfigManager.PluginConfigs.OfType<WindmillConfig>().First();
            var json = JObject.FromObject(stateChange);
            json.Add("IdHex", stateChange.Id.ToString("x2"));
            using var sc = new StringContent(json.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            await client.PostAsync(config.Url, sc, CancellationToken.None);
        }
    }
}
