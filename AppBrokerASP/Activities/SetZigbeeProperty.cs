using AppBroker.Core.Devices;
using AppBroker.ValueProvider;

using AppBrokerASP.Devices.Zigbee;

using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace AppBrokerASP.Activities;

[Activity(Category = "Zigbee", DisplayName = "Setze Zigbee Wert")]
public partial class SetZigbeeProperty : Activity
{
    [ActivityInput(OptionsProvider = typeof(DeviceIdProvider), UIHint = ActivityInputUIHints.Dropdown, DefaultSyntax = SyntaxNames.Literal, SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
    public string? DeviceId { get; set; }

    [ActivityInput(OptionsProvider = typeof(DeviceNameProvider), UIHint = ActivityInputUIHints.Dropdown, DefaultSyntax = SyntaxNames.Literal, SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
    public string? DeviceName { get; set; }

    [ActivityInput(DefaultSyntax = SyntaxNames.Literal, SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Liquid, SyntaxNames.Json, SyntaxNames.JavaScript })]
    public string? PropertyName { get; set; }

    [ActivityInput(DefaultSyntax = SyntaxNames.Literal, SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Liquid, SyntaxNames.Json, SyntaxNames.JavaScript })]
    public object? Value { get; set; }

    protected override IActivityExecutionResult OnExecute()
    {
        return base.OnExecute();
    }

    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
    {
        if (PropertyName is null || Value is null)
            return Done();

        if (DeviceId is null && DeviceName is null)
            return Fault($"Must pass device id or device name");

        Device? device = null;
        if (DeviceId is null
            || (!long.TryParse(DeviceId, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out long id)
            && !long.TryParse(DeviceId, out id))
            || !IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var dev))
        {
            device = IInstanceContainer.Instance.DeviceManager.Devices.Values.FirstOrDefault(x => x.FriendlyName == DeviceName!);

        }

        if (device is null)
            return Fault($"No updateable Zigbee Device found with id {DeviceId} or name {DeviceName}");

        if (device is not UpdateableZigbeeDevice updateable)
        {
            return Fault($"No updateable Zigbee Device found with id {DeviceId}");
        }

        await updateable.SetValue(PropertyName, Value);

        return Done();
    }
}