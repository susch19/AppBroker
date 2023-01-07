using AppBroker.Core;
using AppBroker.Core.Database.Model;
using AppBroker.Core.Database;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;

using Jint;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NiL.JS;
using NiL.JS.Core;

using NLog;

using NonSucking.Framework.Extension.Threading;

using Quartz;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using AppBroker.Core.Models;
using DayOfWeek = AppBroker.Core.Models.DayOfWeek;
using Esprima;
using Jint.Native;
using CoordinateSharp;
using NonSucking.Framework.Extension.Collections;

namespace AppBrokerASP.Devices;

[AppBroker.ClassPropertyChangedAppbroker]
public partial class PropChangedJavaScriptDevice : ConnectionJavaScriptDevice
{
    public PropChangedJavaScriptDevice(FileInfo info) : base(info)
    {
    }

    public PropChangedJavaScriptDevice(long id, string? typeName, FileInfo? info) : base(id, typeName, info)
    {
    }

    protected virtual void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        SetState(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], JToken.FromObject(value));
    }
}

public class ConnectionJavaScriptDevice : JavaScriptDevice
{
    public virtual bool IsConnected
    {
        get; set;
    }
    public ConnectionJavaScriptDevice(FileInfo info) : base(info)
    {
    }

    public ConnectionJavaScriptDevice(long id, string? typeName, params FileInfo[] info) : base(id, typeName, info)
    {
    }

    public void SetConnectionStatus(bool newState)
    {
        IsConnected = newState;
        SetState("isConnected", newState);

    }
    public override void StopDevice() => SetConnectionStatus(false);

    public override void Reconnect(ByteLengthList parameter) => SetConnectionStatus(true);
}

public record struct CommandParameters(Command Command, List<JToken> Parameters);


public class JavaScriptDevice : Device
{
    private class JSFileAndEngine
    {
        public JavaScriptFile JS { get; set; }
        public Context? Engine { get; set; }

        public JSFileAndEngine()
        {

        }

        public JSFileAndEngine(JavaScriptFile jS)
        {
            JS = jS;
        }
    }

    private record struct StateChangedArgs(string Id, string PropertyName, JToken? OldValue, JToken NewValue);

    private event EventHandler<CommandParameters>? OnUpdateFromApp;
    private event EventHandler<CommandParameters>? OnOptionsFromApp;
    private event EventHandler<StateChangedArgs> OnAnyDeviceStateChanged;


    [JsonIgnore]
    public string IdStr { get; set; }

    [JsonIgnore]
    public bool FileBased { get; }

    private readonly HashSet<Guid> runningIntervalls = new();
    private readonly ScopedSemaphore semaphore = new ScopedSemaphore();
    private List<JSFileAndEngine> fileInfos;


    public JavaScriptDevice(FileInfo info) : base(0)
    {
        fileInfos = new()
        {
            new(new JavaScriptFile() { Content = File.ReadAllText(info.FullName) })
        };

        Id = long.Parse(Path.GetFileNameWithoutExtension(info.Name));
        FileBased = true;
    }

    public JavaScriptDevice(long id, string? typeName, params FileInfo[] info) : base(id, typeName)
    {
        Id = id;

        fileInfos = info
            .Where(info => info.Exists)
            .Select(info => new JSFileAndEngine(new JavaScriptFile() with { Content = File.ReadAllText(info.FullName) }))
            .ToList();

        if (fileInfos.Count > 0)
        {
            RebuildEngine();
        }
        FileBased = false;
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        base.UpdateFromApp(command, parameters);
        return Task.Run(() => OnUpdateFromApp?.Invoke(this, new(command, parameters)));
    }

    public override void OptionsFromApp(Command command, List<JToken> parameters)
    {
        base.OptionsFromApp(command, parameters);
        OnOptionsFromApp?.Invoke(this, new(command, parameters));
    }


    public void RebuildEngine()
    {
        if (OnUpdateFromApp is not null)
        {
            foreach (EventHandler<CommandParameters> d in OnUpdateFromApp.GetInvocationList())
                OnUpdateFromApp -= d;
        }

        if (OnOptionsFromApp is not null)
        {
            foreach (EventHandler<CommandParameters> d in OnOptionsFromApp.GetInvocationList())
                OnOptionsFromApp -= d;
        }


        runningIntervalls.Clear();
        var logger = LogManager.GetLogger(FriendlyName + "_" + Id);
        foreach (var item in fileInfos)
        {
            var engine = ExtendEngine(IInstanceContainer.Instance.JavaScriptEngineManager
                .GetNilJSEngineWithDefaults(logger)
            );
            engine.DefineVariable("device").Assign(JSValue.Marshal(this));
            engine.DefineVariable("deviceId").Assign(JSValue.Marshal(Id.ToString()));
            engine.DefineFunction("interval", Interval)
                .DefineFunction("timedTrigger", TimeTrigger)
                .DefineFunction("stopInterval", StopInterval)
                .DefineFunction("onUpdateFromApp", UpdateFromAppJS)
                .DefineFunction("onOptionsFromApp", OptionsFromAppJS)
                .DefineFunction("removeUpdateFromApp", RemoveUpdateFromAppJS)
                .DefineFunction("removeOptionsFromApp", RemoveOptionsFromAppJS)
                .DefineFunction("sendDataToAllSubscribers", SendDataToAllSubscribers)
                .DefineFunction("rebuild", RebuildEngine)
                .DefineFunction("save", StorePersistent)
                .DefineFunction("currentHeaterSetting", CurrentHeaterConfig)
                .DefineFunction("nextHeaterSetting", NextHeaterConfig)
                .DefineFunction("onAnyDeviceStateChanged", SubscribeAnyDeviceStateChanged)
                .DefineFunction("getCoordinateInfo", (float lan, float lon) => new Coordinate(lan, lon, DateTime.Now))
                .DefineFunction("getSunSet", (float lan, float lon) => new Coordinate(lan, lon, DateTime.Now).CelestialInfo.SunSet!)
                .DefineFunction("getSunRise", (float lan, float lon) => new Coordinate(lan, lon, DateTime.Now).CelestialInfo.SunRise!)
                .DefineFunction("getMoonSet", (float lan, float lon) => new Coordinate(lan, lon, DateTime.Now).CelestialInfo.MoonSet!)
                .DefineFunction("getMoonRise", (float lan, float lon) => new Coordinate(lan, lon, DateTime.Now).CelestialInfo.MoonRise!)
                .DefineFunction("executeAt", (DateTime at, Action method) => Task.Delay(DateTime.UtcNow.Subtract(at)).ContinueWith(x => method()))
                ;

            //engine.Debugging = true;
            //engine.DebuggerCallback += Context_DebuggerCallback;
            engine.Eval(item.JS.Content);
            item.Engine = engine;
        }

    }

    public void AnyDeviceStateChanged(StateChangeArgs e)
        => OnAnyDeviceStateChanged?.Invoke(this, new(e.Id.ToString(), e.PropertyName, e.OldValue, e.NewValue));

    private EventHandler<CommandParameters> UpdateFromAppJS(Action<object, CommandParameters> method)
    {
        var evHandler = new EventHandler<CommandParameters>(
            (s, e) =>
            {
                using (semaphore.Wait())
                    method(s, e);
            });
        OnUpdateFromApp += evHandler;
        return evHandler;
    }

    private EventHandler<CommandParameters> OptionsFromAppJS(Action<object, CommandParameters> method)
    {
        EventHandler<CommandParameters>? evHandler = new(
            (s, e) =>
            {
                using (semaphore.Wait())
                    method(s, e);
            });
        OnOptionsFromApp += evHandler;
        return evHandler;
    }

    private void RemoveUpdateFromAppJS(EventHandler<CommandParameters> handler)
        => OnUpdateFromApp -= handler;
    private void RemoveOptionsFromAppJS(EventHandler<CommandParameters> handler)
        => OnOptionsFromApp -= handler;

    private Guid Interval(Action method, int interval)
    {
        var guid = Guid.NewGuid();
        while (!runningIntervalls.Add(guid))
            guid = Guid.NewGuid();
        Task.Run(async () =>
        {
            while (runningIntervalls.Contains(guid))
            {
                using (semaphore.Wait())
                    method();
                await Task.Delay(interval);
            }
        });
        return guid;
    }

    private bool StopInterval(Guid guid) => runningIntervalls.Remove(guid);

    private Guid TimeTrigger(Action method, string cron)
    {
        var schedule = new CronExpression(cron);
        var nextSchedule = schedule.GetTimeAfter(new DateTimeOffset(DateTime.Now))!.Value;

        var guid = Guid.NewGuid();
        while (!runningIntervalls.Add(guid))
            guid = Guid.NewGuid();

        Task.Run(async () =>
        {
            while (true)
            {
                var ms = Math.Max(0, (int)nextSchedule.Subtract(new DateTimeOffset(DateTime.Now)).TotalMilliseconds);
                await Task.Delay(ms);
                if (!runningIntervalls.Contains(guid))
                    return;
                try
                {
                    using (semaphore.Wait())
                        method();
                }
                catch (Exception ex)
                {
                    ;
                }
                nextSchedule = schedule.GetTimeAfter(new DateTimeOffset(DateTime.Now))!.Value;
            }
        });

        return guid;
    }



    //private void Context_DebuggerCallback(Context sender, DebuggerCallbackEventArgs e)
    //{
    //    Console.Clear();
    //    for (var i = 0; i < fileInfos.Content.Length; i++)
    //    {
    //        if (i >= e.Statement.Position && i <= e.Statement.EndPosition)
    //        {
    //            Console.Write(fileInfos.Content[i]);
    //        }

    //    }

    //    Console.WriteLine();

    //    Console.WriteLine("Variables:");
    //    Console.WriteLine(string.Join(Environment.NewLine, new ContextDebuggerProxy(sender).Variables.Select(x => x.Key + ": " + x.Value)));

    //    Console.WriteLine();
    //    Console.WriteLine("Output:");

    //}

    public IHeaterConfigModel? CurrentHeaterConfig()
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        DeviceModel? d = cont.Devices
            .Include(x => x.HeaterConfigs)
            .ThenInclude(x => x.HeatingPlan)
            .FirstOrDefault(x => x.Id == Id);

        if (d is null || d.HeaterConfigs is null || d.HeaterConfigs.Count < 1)
            return null;
        IHeaterConfigModel? bestFit = null;

        var curDow = (DayOfWeek)((int)(DateTime.Now.DayOfWeek + 6) % 7);
        var curTimeOfDay = DateTime.Now.TimeOfDay;
        foreach (var item in d.HeaterConfigs
            .Where(x => x.HeatingPlan is null || x.HeatingPlan.Active)
            .OrderByDescending(x => x.DayOfWeek)
            .ThenByDescending(x => x.TimeOfDay))
        {
            bestFit = item;

            if ((item.DayOfWeek == curDow
                    && item.TimeOfDay.TimeOfDay < curTimeOfDay)
                || item.DayOfWeek < curDow)
            {
                break;
            }
        }
        return bestFit;
    }

    private IHeaterConfigModel? NextHeaterConfig()
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        DeviceModel? d = cont.Devices
            .Include(x => x.HeaterConfigs)
            .FirstOrDefault(x => x.Id == Id);

        if (d is null || d.HeaterConfigs is null || d.HeaterConfigs.Count < 1)
            return null;
        IHeaterConfigModel? bestFit = null;

        var curDow = (DayOfWeek)((int)(DateTime.Now.DayOfWeek + 6) % 7);
        var curTimeOfDay = DateTime.Now.TimeOfDay;
        foreach (var item in d.HeaterConfigs.OrderByDescending(x => x.DayOfWeek).ThenByDescending(x => x.TimeOfDay))
        {
            bestFit ??= item;

            if ((item.DayOfWeek == curDow
                 && item.TimeOfDay.TimeOfDay < curTimeOfDay)
                || item.DayOfWeek < curDow)
            {
                break;
            }
            bestFit = item;
        }
        return bestFit;
    }



    private EventHandler<StateChangedArgs> SubscribeAnyDeviceStateChanged(Action<string, string, JToken?, JToken> method)
    {
        var evHandler = new EventHandler<StateChangedArgs>(
            (s, e) =>
            {
                using (semaphore.Wait())
                    method(e.Id, e.PropertyName, e.OldValue, e.NewValue);
            });
        OnAnyDeviceStateChanged += evHandler;
        return evHandler;
    }


    protected virtual Engine ExtendEngine(Engine engine)
    {
        return engine;
    }
    protected virtual Context ExtendEngine(Context engine)
    {
        return engine;
    }
}

/*
Field Name 	    Mandatory 	Allowed Values 	    Allowed Special Characters
Seconds 	    YES 	    0-59 	            , - * /
Minutes 	    YES 	    0-59 	            , - * /
Hours 	        YES 	    0-23 	            , - * /
Day of month 	YES 	    1-31 	            , - * ? / L W
Month 	        YES 	    1-12 or JAN-DEC 	, - * /
Day of week 	YES 	    1-7 or SUN-SAT 	    , - * ? / L #
Year 	        NO 	        empty, 1970-2099 	, - * /
 */