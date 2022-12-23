

using IoBrokerHistoryImporter;

using Microsoft.EntityFrameworkCore;

using System.Text.RegularExpressions;

Console.WriteLine("Please enter the path for the app broker db context file");
var ownDbContextPath = Console.ReadLine();
//var ownDbContextPath = @"C:\Users\susch\source\repos\AppBroker\AppBrokerASP\history - Kopie.db"; 
Console.WriteLine("Please enter the path for the io broker db context file");
var ioBrokerDbContextPath = Console.ReadLine();
//var ioBrokerDbContextPath = @"C:\Users\susch\source\repos\AppBroker\AppBrokerASP\sqlite.db";

List<HistoryValueBase> values = new(1000000);

using var appContext = new HistoryContext(ownDbContextPath);
appContext.Database.Migrate();

using var ioContext = new IoBrokerHistoryContext(ioBrokerDbContextPath);
//ioContext.Database.Migrate();
Dictionary<string, HistoryProperty> createdDevices = new();
int counter = 0;

var dps = ioContext.Datapoints.ToDictionary(x => x.Id, x => x);

foreach (var item in ioContext.Bools.OrderByDescending(x=>x.Ts))
{
    HistoryProperty? histProp = GetOrCreateHistProp(createdDevices, dps[item.Id]);

    var newBool = new HistoryValueBool(item.Val);
    AddNewValue(item.Ts, histProp, newBool);
}

foreach (var item in ioContext.Strings.OrderByDescending(x => x.Ts))
{
    HistoryProperty? histProp = GetOrCreateHistProp(createdDevices, dps[item.Id]);

    var newBool = new HistoryValueString(item.Val);
    AddNewValue(item.Ts, histProp, newBool);
}

foreach (var item in ioContext.Doubles)
{
    HistoryProperty? histProp = GetOrCreateHistProp(createdDevices, dps[item.Id]);

    if ((long)item.Val == item.Val)
    {

        var newBool = new HistoryValueLong((long)item.Val);
        AddNewValue(item.Ts, histProp, newBool);
    }
    else
    {
        var newBool = new HistoryValueDouble(item.Val);
        AddNewValue(item.Ts, histProp, newBool);
    }
}


appContext.AddRange(values);
appContext.SaveChanges();

Console.WriteLine("Finito");
Console.ReadKey();

HistoryProperty GetOrCreateHistProp(Dictionary<string, HistoryProperty> createdDevices, Datapoint item)
{
    if (!createdDevices.TryGetValue(item.Name, out var histProp))
    {
        var splitted = item.Name.Split('.');
        var propNameWrong = splitted.Last();
        var propName = Regex.Replace(propNameWrong, "_([a-z])", x => x.Value[1..].ToUpperInvariant());
        var deviceIdHex = splitted[splitted.Length - 2];
        var id = long.Parse(deviceIdHex, System.Globalization.NumberStyles.HexNumber);
        var existing = appContext.Properties.FirstOrDefault(x => x.PropertyName == propName && x.Device.DeviceId == id);
        if (existing is null)
        {
            var existingDevice = appContext.Devices.FirstOrDefault(x => x.DeviceId == id);
            if (existingDevice is null)
            {
                existingDevice = new HistoryDevice() { DeviceId = id };
                appContext.Devices.Add(existingDevice);
                appContext.SaveChanges();
            }

            var newHistProp = appContext.Properties.Add(new HistoryProperty() { Device = existingDevice, PropertyName = propName, Enabled = true });
            appContext.SaveChanges();
            existing = newHistProp.Entity;
        }
        histProp = createdDevices[item.Name] = existing;

    }

    return histProp;
}

void AddNewValue(long ts, HistoryProperty histProp, HistoryValueBase baseValue)
{

    baseValue.Timestamp = DateTime.UnixEpoch.AddMilliseconds(ts);
    baseValue.HistoryValue = histProp;
    values.Add(baseValue);
    counter++;
    //Console.WriteLine($"Added new for Date {ts} : {baseValue.Timestamp.ToString("dd.MM.yyyy HH:mm:ss")}");
    if (counter % 10000 == 0)
        Console.WriteLine($"Added {counter} records");
    if (counter % 100000 == 0)
    {
        appContext.AddRange(values);
        appContext.SaveChanges();
        values.Clear();
        Console.WriteLine($"Saved {counter} records");
    }
}