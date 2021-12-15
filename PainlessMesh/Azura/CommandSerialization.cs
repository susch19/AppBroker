using AppBroker.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PainlessMesh;

public static class CommandSerialization
{
    public static Command Deserialize(Stream stream) => Enum.Parse<Command>(StringUShortSerialization.Deserialize(stream));
    public static void Deserialize(Stream stream, out Command self) => self = Deserialize(stream);
    public static void Serialize(this Command self, Stream stream) => Serialize(in self, stream);
    public static void Serialize(in Command self, Stream stream) => StringUShortSerialization.Serialize(self.ToString(), stream);
}
