using AppBroker.Core.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppBroker.Notifications.Configuration;
public record struct FirebaseOptions(string apiKey, string appId, string messagingSenderId, string projectId, string storageBucket, string? iosBundleId = null, string? authDomain = null);
public class FirebaseConfig : IConfig
{
    public string Name => "Firebase";

    public Dictionary<string, FirebaseOptions> Options { get; set; } = new();
}
