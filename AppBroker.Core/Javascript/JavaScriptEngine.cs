using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;

using AppBrokerASP.Devices;

using Jint;

using Newtonsoft.Json.Linq;

using NiL.JS;
using NiL.JS.Core;

using NLog;

using NonSucking.Framework.Extension.Threading;

using System.Collections.Concurrent;

using ILogger = NLog.ILogger;

namespace AppBroker.Core.Javascript;
public class JavaScriptEngineManager
{
    public ConcurrentDictionary<string, Delegate> ExtendedFunctions { get; } = new();
    public ConcurrentDictionary<string, JSValue> ExtendedVariables { get; } = new();
    public ConcurrentDictionary<string, (Func<object> getter, Action<object> setter)> ExtendedGetSetVariables { get; } = new();
    public ConcurrentBag<Type> ExtendedCtors { get; } = new();

    private readonly HttpClient client;

    private readonly ConcurrentDictionary<string, JavaScriptFile> files;
    private readonly ScopedSemaphore filesSemaphore = new();
    private readonly ILogger logger;
    private readonly DirectoryInfo jsDeviceDirectory;
    private readonly DirectoryInfo scriptsDirectory;
    private readonly FileSystemWatcher devicesWatcher;
    private readonly FileSystemWatcher scriptsWatcher;


    public JavaScriptEngineManager()
    {
        logger = LogManager.GetCurrentClassLogger();
        jsDeviceDirectory = new DirectoryInfo("./JSDevices");
        jsDeviceDirectory.Create();
        scriptsDirectory = new DirectoryInfo("./Scripts");
        scriptsDirectory.Create();

        devicesWatcher = new FileSystemWatcher(jsDeviceDirectory.FullName, "*.js")
        {
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Security
        };
        devicesWatcher.Renamed += DeviceChanged;
        devicesWatcher.Deleted += DeviceChanged;
        devicesWatcher.Changed += DeviceChanged;
        devicesWatcher.Created += DeviceChanged;
        devicesWatcher.EnableRaisingEvents = true;

        scriptsWatcher = new FileSystemWatcher(scriptsDirectory.FullName, "*.js")
        {
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Security
        };
        scriptsWatcher.Renamed += ScriptRenamed;
        scriptsWatcher.Deleted += ScriptChanged;
        scriptsWatcher.Changed += ScriptChanged;
        scriptsWatcher.Created += ScriptChanged;
        scriptsWatcher.EnableRaisingEvents = true;

        files = new();

        foreach (var file in scriptsDirectory.GetFiles("*.js"))
        {
            files.TryAdd(file.Name,
                new JavaScriptFile(File.ReadAllText(file.FullName), GetNilJSEngineWithDefaults(logger)));
        }

        client = new();
    }

    public void Initialize()
    {
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
        ReloadJsDevices(false);
    }

    public void ReloadJsDevices(bool onlyLoadNew)
    {
        var scriptFiles = jsDeviceDirectory.GetFiles("*.js", SearchOption.AllDirectories);
        IInstanceContainer.Instance.DeviceManager.Devices.Where(x =>
            x.Value is JavaScriptDevice && ((JavaScriptDevice)x.Value).FileBased)
            .Select(x => x.Key)
            .ToList()
            .ForEach(x => IInstanceContainer.Instance.DeviceManager.RemoveDevice(x));

        foreach (var item in scriptFiles)
        {
            var dv = new JavaScriptDevice(item);
            if (!onlyLoadNew)
                IInstanceContainer.Instance.DeviceManager.RemoveDevice(dv.Id);

            if (IInstanceContainer.Instance.DeviceManager.AddNewDevice(dv))
                dv.RebuildEngine();
            else
                dv.Dispose();

        }
    }

    public Context GetNilJSEngineWithDefaults(ILogger logger)
    {

        var jsEngine = new Context();
        jsEngine.DefineVariable("newtonsoftLinq").Assign(new NamespaceProvider("Newtonsoft.Json.Linq"));
        jsEngine.DefineConstructor(typeof(LogLevel));
        jsEngine
            .DefineFunction("log", new Action<object>(logger.Trace))
            .DefineFunction("logWithLevel", new Action<LogLevel, object>(logger.Log))
            .DefineFunction("setState", new Action<string, string, JSValue>((id, name, val) => IInstanceContainer.Instance.DeviceStateManager.SetSingleState(long.Parse(id), name, JToken.FromObject(val.Value))))
            .DefineFunction("getState", new Func<string, string, object?>((id, name) => IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue(long.Parse(id), name)))
            .DefineFunction("setTimeout", (Action method, int delay) => Task.Delay(delay).ContinueWith((t) => method?.Invoke()))
            .DefineFunction("forceGC", () => GC.Collect())
            .DefineFunction("httpGet", (string s) => client.GetAsync(s).Result)
            .DefineFunction("httpPost", (string s, HttpContent? content) => client.PostAsync(s, content).Result)
            .DefineFunction("reloadDynamicLayouts", DeviceLayoutService.ReloadLayouts)
            .DefineFunction("padLeft", (string s, string paddingChar, int count) => s.PadLeft(count, paddingChar[0]))
            .DefineFunction("padRight", (string s, string paddingChar, int count) => s.PadRight(count, paddingChar[0]))
        ;
        foreach (var item in ExtendedFunctions)
            jsEngine.DefineFunction(item.Key, item.Value);

        foreach (var item in ExtendedCtors)
            jsEngine.DefineConstructor(item);

        foreach (var item in ExtendedVariables)
            jsEngine.DefineVariable(item.Key).Assign(item.Value);

        foreach (var item in ExtendedGetSetVariables)
            jsEngine.DefineGetSetVariable(item.Key, item.Value.getter, item.Value.setter);

        return jsEngine;
    }

    public Engine GetEngineWithDefaults(ILogger logger)
    {
        return new Engine(cfg => cfg.AllowClr(
            typeof(JavaScriptEngineManager).Assembly,
            typeof(Device).Assembly,
            typeof(LogLevel).Assembly).AllowClrWrite()) //TODO add more assemblies, so we can call methods on them?
        .SetValue("log", new Action<object>(logger.Trace))
        .SetValue("logWithLevel", new Action<LogLevel, object>(logger.Log))
        .SetValue("setState", new Action<long, string, JToken, StateFlags>(IInstanceContainer.Instance.DeviceStateManager.SetSingleState))
        .SetValue("getState", new Func<long, string, object?>(IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue))

        ; //Add more easy to use methods
    }


    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        foreach (var item in files)
        {
            item.Value.Engine.DefineVariable("State").Assign(JSValue.Marshal(e));
            try
            {
                item.Value.Engine.Eval(item.Value.Content);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during execution of " + item.Key);
            }
            finally
            {
                //engines.Enqueue(engines);
            }
        }


        foreach (var item in IInstanceContainer.Instance.DeviceManager.Devices)
        {
            if (item.Value is JavaScriptDevice jsDevice)
            {
                jsDevice.AnyDeviceStateChanged(e);
            }
        }

    }
    private void ScriptChanged(object sender, FileSystemEventArgs e)
    {
        if (e.Name is null)
            return;

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                var content = File.ReadAllText(e.FullPath);
                files[e.Name] = new(content, GetNilJSEngineWithDefaults(logger));
                break;
            case WatcherChangeTypes.Deleted:
                files.Remove(e.Name, out _);
                break;
            case WatcherChangeTypes.Changed:
                var newContent = File.ReadAllText(e.FullPath);

                files[e.Name] = files[e.Name] with { Content = newContent };
                break;

        }
    }
    private void ScriptRenamed(object sender, RenamedEventArgs e)
    {
        if (e.OldName is null || e.Name is null)
            return;

        if (files.Remove(e.OldName, out var info))
        {
            files[e.Name] = info;
        }
    }

    private void DeviceChanged(object sender, FileSystemEventArgs e)
    {
        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                ReloadJsDevices(true);
                break;

            case WatcherChangeTypes.Changed:
            case WatcherChangeTypes.Renamed:
            case WatcherChangeTypes.Deleted:
                ReloadJsDevices(false);
                break;
        }
    }
}
