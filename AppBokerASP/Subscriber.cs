﻿using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP
{
    public class Subscriber
    {
        public string ConnectionId { get; set; }
        public ISmartHomeClient SmarthomeClient { get; internal set; }
        public Subscriber(string connectionId, ISmartHomeClient smarthomeClient)
        {
            ConnectionId = connectionId;
            SmarthomeClient = smarthomeClient;
        }


    }
}