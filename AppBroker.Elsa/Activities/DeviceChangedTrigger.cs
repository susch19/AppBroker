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
using AppBroker.ValueProvider;

namespace AppBroker.Activities;

[Trigger(
   Category = "Smarthome",
   Description = "Waits for a property change on a specified device."
)]
public partial class DeviceChangedTrigger : Activity
{
    [ActivityInput(Label = "Property Name", OptionsProvider = typeof(PropertyChangedTrigger), UIHint = ActivityInputUIHints.Dropdown,
      SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
    public string PropertyName { get; set; } = default!;

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
    public DeviceChangedEvent? Output { get; set; }
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        => context.IsFirstPass ? OnExecuteInternal(context) : Suspend();

    protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        => OnExecuteInternal(context);

    private IActivityExecutionResult OnExecuteInternal(ActivityExecutionContext context)
    {
        var input = context.GetInput<DeviceChangedEvent>();
        Output = input;
        return Done(Output);
    }

}
