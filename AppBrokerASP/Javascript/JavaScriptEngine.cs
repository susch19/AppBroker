using Jint;

using Newtonsoft.Json.Linq;

namespace AppBrokerASP.Javascript;
public class JavaScriptEngineManager
{
    private readonly Engine engine = new Engine(cfg => cfg.AllowClr(typeof(JavaScriptEngineManager).Assembly)) //TODO add more assemblies, so we can call methods on them?
        .SetValue("log", new Action<object>(Console.WriteLine))
        .SetValue("setState", new Action<long, string, JToken>(InstanceContainer.Instance.DeviceStateManager.SetSingleState))
        .SetValue("getState", new Func<long, string, JToken?>(InstanceContainer.Instance.DeviceStateManager.GetSingleState))
        
        ; //Add more easy to use methods

    private readonly List<JavaScriptFile> files = new();

    public void Initialize()
    {
        InstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
    }

    private void DeviceStateManager_StateChanged(object? sender, Zigbee2Mqtt.StateChangeArgs e)
    {
        var currentFiles = Directory.GetFiles("./Scripts", "*.js").Select(x=>new FileInfo(x)).ToArray();
        System.Console.WriteLine();
        for (int i = 0; i < currentFiles.Length; i++)
        {
            FileInfo item = currentFiles[i];
            if (!files.Any(x => x.File.FullName == item.FullName))
                files.Add(new() { File = item });
        }
        for (int i = files.Count - 1; i >= 0; i--)
        {
            JavaScriptFile item = files[i];
            if (!currentFiles.Any(x=>x.FullName == item.File.FullName))
            {
                files.Remove(item);
                continue;
            }
            
            engine.SetValue("State", e);
            item.File.Refresh();
            if (item.File.LastWriteTimeUtc != item.LastWriteTimeUtc)
            {
                files.Remove(item);
                item = item with { Content = File.ReadAllText(item.File.FullName), LastWriteTimeUtc = item.File.LastWriteTimeUtc };
                files.Add(item);
            }
            engine.Execute(item.Content);
        }
    }
}
