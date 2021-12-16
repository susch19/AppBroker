using Elsa;
using AppBroker.Elsa.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

using System.Collections.Generic;
using System.Linq;
using Elsa.Metadata;
using System.Reflection;
using AppBrokerASP;
using AppBroker.ValueProvider;

namespace AppBroker.Activities;

[Activity(
   Category = "Smarthome",
   Description = "Gets an instance of a device.",
    DisplayName = "Get Device"
)]
public class GetDeviceActivity : Activity
{

    [ActivityInput(
        Label = "Device Name", OptionsProvider = typeof(DeviceNameProvider), UIHint = ActivityInputUIHints.Dropdown,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string DeviceName { get; set; } = default!;

    [ActivityInput(
        Label = "Type Name", OptionsProvider = typeof(DeviceTypeNameProvider),
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }, UIHint = ActivityInputUIHints.Dropdown)]
    public string TypeName { get; set; } = default!;

    [ActivityInput(
        Label = "Device Id", OptionsProvider = typeof(DeviceIdProvider), UIHint = ActivityInputUIHints.Dropdown,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public long? DeviceId { get; set; } = default!;

    [ActivityOutput]
    public GetDeviceEvent? Output { get; set; }
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        => OnExecuteInternal(context);

    private IActivityExecutionResult OnExecuteInternal(ActivityExecutionContext context)
    {
        var deviceFindQuery = IInstanceContainer.Instance.DeviceManager.Devices.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(DeviceName))
            deviceFindQuery = deviceFindQuery.Where(x => x.FriendlyName == DeviceName);
        if (!string.IsNullOrWhiteSpace(TypeName))
            deviceFindQuery = deviceFindQuery.Where(x => x.TypeName == TypeName);
        if (DeviceId.HasValue)
            deviceFindQuery = deviceFindQuery.Where(x => x.Id == DeviceId);

        var device = deviceFindQuery.FirstOrDefault();

        if (device is not null)
        {
            Output = new GetDeviceEvent(device);

            return Done(Output);
        }

        return Fault("No Device with Parameters could be found");
    }
}
