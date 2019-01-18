using System.Collections.Generic;

namespace AppBokerASP
{


    public class Rootobject
    {
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
