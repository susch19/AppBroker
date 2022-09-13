﻿using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;

using AppBrokerASP.Javascript;

using Jint;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NiL.JS;
using NiL.JS.Core;

using NLog;

using NonSucking.Framework.Extension.Threading;

using Quartz;

using System.Runtime.CompilerServices;

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
    private bool isConnected;

    [JsonIgnore]
    public virtual bool Connected
    {
        get => isConnected;
        set
        {
            isConnected = value;
            SetState("isConnected", value);
        }
    }
    public ConnectionJavaScriptDevice(FileInfo info) : base(info)
    {
        Connected = true;
    }

    public ConnectionJavaScriptDevice(long id, string? typeName, FileInfo? info) : base(id, typeName, info)
    {
        Connected = true;
    }

    public override void StopDevice() => Connected = false;
    public override void Reconnect(ByteLengthList parameter) => Connected = true;
}

public record struct CommandParameters(Command Command, List<JToken> Parameters);

public class JavaScriptDevice : Device
{
    private event EventHandler<CommandParameters>? OnUpdateFromApp;
    private event EventHandler<CommandParameters>? OnOptionsFromApp;

    public override long Id { get; set; }
    public override string TypeName { get; set; }
    public override bool ShowInApp { get; set; }
    public override string FriendlyName { get; set; }


    private readonly HashSet<Guid> runningIntervalls = new();
    private readonly ScopedSemaphore semaphore = new ScopedSemaphore();
    private JavaScriptFile fileInfo;
    private Context engine;


    public JavaScriptDevice(FileInfo info) : base(0)
    {
        fileInfo = new JavaScriptFile() with { File = info, LastWriteTimeUtc = info.LastWriteTimeUtc, Content = File.ReadAllText(info.FullName) };
        Id = long.Parse(Path.GetFileNameWithoutExtension(info.Name));
        //client.GetAsync("").Result.Content.ReadAsStringAsync()
        RebuildEngine();
    }

    public JavaScriptDevice(long id, string? typeName, FileInfo? info) : base(id, typeName)
    {
        Id = id;
        if (info is not null)
        {
            fileInfo = new JavaScriptFile() with { File = info };
            if (info.Exists)
            {
                fileInfo = fileInfo with { LastWriteTimeUtc = info.LastWriteTimeUtc, Content = File.ReadAllText(info.FullName) };
                RebuildEngine();
            }
        }
        //client.GetAsync("").Result.Content.ReadAsStringAsync()
    }

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


    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    => Task.Run(() => OnUpdateFromApp?.Invoke(this, new(command, parameters)));

    public override void OptionsFromApp(Command command, List<JToken> parameters)
        => OnOptionsFromApp?.Invoke(this, new(command, parameters));

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

    public bool HasChanges()
    {
        fileInfo.File.Refresh();
        return fileInfo.File.LastWriteTimeUtc > fileInfo.LastWriteTimeUtc;

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

        if (HasChanges())
            fileInfo = fileInfo with { LastWriteTimeUtc = fileInfo.File.LastWriteTimeUtc, Content = File.ReadAllText(fileInfo.File.FullName) };

        runningIntervalls.Clear();
        var logger = LogManager.GetLogger(FriendlyName + "_" + Id);
        engine = ExtendEngine(IInstanceContainer.Instance.JavaScriptEngineManager
            .GetNilJSEngineWithDefaults(logger)
            );
        engine.DefineVariable("device").Assign(JSValue.Marshal(this));
        engine.DefineFunction("interval", Interval)
            .DefineFunction("timedTrigger", TimeTrigger)
            .DefineFunction("stopInterval", StopInterval)
            .DefineFunction("onUpdateFromApp", UpdateFromAppJS)
            .DefineFunction("onOptionsFromApp", OptionsFromAppJS)
            .DefineFunction("removeUpdateFromApp", RemoveUpdateFromAppJS)
            .DefineFunction("removeOptionsFromApp", RemoveOptionsFromAppJS)
            .DefineFunction("sendDataToAllSubscribers", SendDataToAllSubscribers)
            .DefineFunction("checkForChanges", HasChanges)
            .DefineFunction("rebuild", RebuildEngine)
            ;

        //engine.Debugging = true;
        //engine.DebuggerCallback += Context_DebuggerCallback;
        engine.Eval(fileInfo.Content);
    }

    private void Context_DebuggerCallback(Context sender, DebuggerCallbackEventArgs e)
    {
        Console.Clear();
        for (var i = 0; i < fileInfo.Content.Length; i++)
        {
            if (i >= e.Statement.Position && i <= e.Statement.EndPosition)
            {
                Console.Write(fileInfo.Content[i]);
            }

        }

        Console.WriteLine();

        Console.WriteLine("Variables:");
        Console.WriteLine(string.Join(Environment.NewLine, new ContextDebuggerProxy(sender).Variables.Select(x => x.Key + ": " + x.Value)));

        Console.WriteLine();
        Console.WriteLine("Output:");

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