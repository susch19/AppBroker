using Microsoft.AspNetCore.SignalR;

namespace AppBroker.Core.Data
{
    public class Subscriber
    {
        public string ConnectionId { get; set; }
        public IClientProxy ClientProxy { get; set; }
    }
}