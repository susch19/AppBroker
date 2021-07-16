using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PainlessMesh
{
    public static class MessageTypeSerialization
    {
        public static MessageType Deserialize(Stream stream)
        {
            return Enum.Parse<MessageType>(StringUShortSerialization.Deserialize(stream));
        }
        public static void Deserialize(Stream stream, out MessageType self) => self = Deserialize(stream);
        public static void Serialize(this MessageType self, Stream stream) => Serialize(in self, stream);
        public static void Serialize(in MessageType self, Stream stream)
        {
            StringUShortSerialization.Serialize(self.ToString(), stream);
        }
    }    
    
    public static class BinarySmarthomeMessageSerialization
    {
        public static BinarySmarthomeMessage Deserialize(Stream stream)
        {
            var bsm = new BinarySmarthomeMessage();
            bsm.Header = PainlessMesh.SmarthomeHeaderSerialization.Deserialize(stream);
            //bsm.NodeId = uintSerialization.Deserialize(stream);
            bsm.MessageType = PainlessMesh.MessageTypeSerialization.Deserialize(stream);
            bsm.Command = PainlessMesh.CommandSerialization.Deserialize(stream);
            bsm.Parameters = PainlessMesh.ByteLengthListSerialization.Deserialize(stream);
            return bsm;
        }
        public static void Deserialize(Stream stream, out BinarySmarthomeMessage self) => self = Deserialize(stream);
        public static void Serialize(this BinarySmarthomeMessage self, Stream stream) => Serialize(in self, stream);
        public static void Serialize(in BinarySmarthomeMessage self, Stream stream)
        {
            PainlessMesh.SmarthomeHeaderSerialization.Serialize(self.Header, stream);
            //uintSerialization.Serialize(self.NodeId, stream);
            PainlessMesh.MessageTypeSerialization.Serialize(self.MessageType, stream);
            PainlessMesh.CommandSerialization.Serialize(self.Command, stream);
            PainlessMesh.ByteLengthListSerialization.Serialize(self.Parameters, stream);
        }
    }
}
