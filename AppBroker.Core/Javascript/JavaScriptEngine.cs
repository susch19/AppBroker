using AppBroker.Core.Devices;

using AppBrokerASP.Devices;

using Jint;

using Newtonsoft.Json.Linq;

using NLog;

using System.Collections.Concurrent;

using ILogger = NLog.ILogger;

namespace AppBrokerASP.Javascript;
public class JavaScriptEngineManager
{
    private readonly ConcurrentQueue<Engine> engines = new();


    public Engine GetEngineWithDefaults(ILogger logger)
    {

        return new Engine(cfg => cfg.AllowClr(
            typeof(JavaScriptEngineManager).Assembly,
            typeof(Device).Assembly,
            typeof(NLog.LogLevel).Assembly).AllowClrWrite()) //TODO add more assemblies, so we can call methods on them?
        .SetValue("log", new Action<object>(logger.Trace))
        .SetValue("logWithLevel", new Action<NLog.LogLevel, object>(logger.Log))
        .SetValue("setState", new Action<long, string, JToken>(IInstanceContainer.Instance.DeviceStateManager.SetSingleState))
        .SetValue("getState", new Func<long, string, object?>(IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue))

        ; //Add more easy to use methods
    }

    private readonly List<JavaScriptFile> files = new();
    private ILogger logger;

    public JavaScriptEngineManager()
    {
        logger = LogManager.GetCurrentClassLogger();
    }

    public void Initialize()
    {
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
        var scriptFiles = Directory.GetFiles("./JSDevices", "*.js", SearchOption.AllDirectories);
        foreach (var item in scriptFiles)
        {
            var dv = new JavaScriptDevice(new FileInfo(item));
            IInstanceContainer.Instance.DeviceManager.AddNewDevice(dv);
        }
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        var currentFiles = Directory.GetFiles("./Scripts", "*.js").Select(x => new FileInfo(x)).ToArray();

        for (int i = 0; i < currentFiles.Length; i++)
        {
            FileInfo item = currentFiles[i];
            if (!files.Any(x => x.File.FullName == item.FullName))
                files.Add(new() { File = item });
        }
        for (int i = files.Count - 1; i >= 0; i--)
        {
            JavaScriptFile item = files[i];
            if (!currentFiles.Any(x => x.FullName == item.File.FullName))
            {
                files.Remove(item);
                continue;
            }

            if (!engines.TryDequeue(out var engine))
                engine = GetEngineWithDefaults(logger);

            engine.SetValue("State", e);
            item.File.Refresh();
            if (item.File.LastWriteTimeUtc != item.LastWriteTimeUtc)
            {
                files.Remove(item);
                item = item with { Content = File.ReadAllText(item.File.FullName), LastWriteTimeUtc = item.File.LastWriteTimeUtc };
                files.Add(item);
            }
            try
            {
                engine.Execute(item.Content);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during execution of " + item.File.Name);
            }
            finally
            {
                engines.Enqueue(engine);
            }
        }
    }
}
