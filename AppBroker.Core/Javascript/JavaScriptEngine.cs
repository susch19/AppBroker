using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;

using AppBrokerASP.Devices;

using Jint;

using Newtonsoft.Json.Linq;

using NiL.JS;
using NiL.JS.Core;

using NLog;

using NonSucking.Framework.Extension.Threading;

using System.Collections.Concurrent;

using ILogger = NLog.ILogger;

namespace AppBrokerASP.Javascript;
public class JavaScriptEngineManager
{
    private readonly ConcurrentQueue<Engine> engines = new();


    public Context GetNilJSEngineWithDefaults(ILogger logger)
    {

        var jsEngine = new Context();
        jsEngine.DefineVariable("newtonsoftLinq").Assign(new NamespaceProvider("Newtonsoft.Json.Linq"));
        jsEngine.DefineConstructor(typeof(LogLevel));
        jsEngine.DefineFunction("log", new Action<object>(logger.Trace))
        .DefineFunction("logWithLevel", new Action<NLog.LogLevel, object>(logger.Log))
        .DefineFunction("setState", new Action<long, string, JSValue>((id, name, val) => IInstanceContainer.Instance.DeviceStateManager.SetSingleState(id, name, JToken.FromObject(val.Value))))
        .DefineFunction("getState", new Func<long, string, object?>(IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue));
        return jsEngine;
    }

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
    private readonly ScopedSemaphore filesSemaphore = new();
    private readonly ILogger logger;
    private readonly DirectoryInfo jsDeviceDirectory;
    private readonly DirectoryInfo scriptsDirectory;

    public JavaScriptEngineManager()
    {
        logger = LogManager.GetCurrentClassLogger();
        jsDeviceDirectory = new DirectoryInfo("./JSDevices");
        jsDeviceDirectory.Create();
        scriptsDirectory = new DirectoryInfo("./Scripts");
    }

    public void Initialize()
    {
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
        var scriptFiles = jsDeviceDirectory.GetFiles("*.js", SearchOption.AllDirectories);
        foreach (var item in scriptFiles)
        {
            var dv = new JavaScriptDevice(item);
            IInstanceContainer.Instance.DeviceManager.AddNewDevice(dv);
        }
    }
    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (!scriptsDirectory.Exists)
            return;

        var currentFiles = scriptsDirectory.GetFiles("*.js").ToArray();
        using (filesSemaphore.Wait())
            for (int i = 0; i < currentFiles.Length; i++)
            {
                FileInfo item = currentFiles[i];
                if (!files.Any(x => x.File.FullName == item.FullName))
                    files.Add(new() { File = item });
            }

        for (int i = files.Count - 1; i >= 0; i--)
        {
            JavaScriptFile item = files[i];
            using (filesSemaphore.Wait())
                if (!currentFiles.Any(x => x.FullName == item.File.FullName))
                {
                    files.Remove(item);
                    continue;
                }

            item.File.Refresh();
            if (item.File.LastWriteTimeUtc > item.LastWriteTimeUtc)
            {
                using var _ = filesSemaphore.Wait();
                files.Remove(item);
                item = item with { Content = File.ReadAllText(item.File.FullName), LastWriteTimeUtc = item.File.LastWriteTimeUtc, Engine = GetNilJSEngineWithDefaults(logger) };
                files.Add(item);
            }

            item.Engine.DefineVariable("State").Assign(JSValue.Marshal(e));
            try
            {
                item.Engine.Eval(item.Content);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during execution of " + item.File.Name);
            }
            finally
            {
                //engines.Enqueue(engine);
            }
        }
    }
    private void DeviceStateManager_StateChangedOld(object? sender, StateChangeArgs e)
    {
        if (!scriptsDirectory.Exists)
            return;

        var currentFiles = scriptsDirectory.GetFiles("*.js").ToArray();
        using (filesSemaphore.Wait())
            for (int i = 0; i < currentFiles.Length; i++)
            {
                FileInfo item = currentFiles[i];
                if (!files.Any(x => x.File.FullName == item.FullName))
                    files.Add(new() { File = item });
            }

        for (int i = files.Count - 1; i >= 0; i--)
        {
            JavaScriptFile item = files[i];
            using (filesSemaphore.Wait())
                if (!currentFiles.Any(x => x.FullName == item.File.FullName))
                {
                    files.Remove(item);
                    continue;
                }

            using var engine = GetEngineWithDefaults(logger);
            engine.SetValue("State", e);
            item.File.Refresh();
            if (item.File.LastWriteTimeUtc > item.LastWriteTimeUtc)
            {
                using var _ = filesSemaphore.Wait();
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
                //engines.Enqueue(engine);
            }
        }
    }
}
