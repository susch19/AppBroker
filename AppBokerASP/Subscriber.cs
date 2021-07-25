using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP
{
    public class Subscriber
    {
        public string ConnectionId { get; set; }
        public IClientProxy ClientProxy { get; internal set; }
        public Subscriber(string connectionId, IClientProxy clientProxy)
        {
            ConnectionId = connectionId;
            ClientProxy = clientProxy;
        }


    }
}