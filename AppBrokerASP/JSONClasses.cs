using System.Collections.Generic;

namespace AppBrokerASP
{


    public class Rootobject
    {
        public Rootobject(uint id, string messageType, string command, List<string> parameters)
        {
            this.id = id;
            MessageType = messageType;
            Command = command;
            Parameters = parameters;
        }

        public uint id { get; set; }
        public string MessageType { get; set; }
        public string Command { get; set; }
        public List<string> Parameters { get; set; }
    }

    //public class Parameter
    //{
    //    public List<string> Parameters[]
    //}

}
