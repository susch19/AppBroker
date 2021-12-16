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

namespace AppBroker.Activities;

[Trigger(
   Category = "Smarthome",
    DisplayName = "Property Changed",
   Description = "Waits for the defined property to change on any device, that has this property."
)]
public class PropertyChangedTrigger : Activity, IActivityPropertyOptionsProvider
{
    [ActivityInput(Label = "Property Name", OptionsProvider = typeof(PropertyChangedTrigger), UIHint = ActivityInputUIHints.Dropdown,
      SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
    public string PropertyName { get; set; } = default!;

    [ActivityOutput]
    public PropertyChangedEvent? Output { get; set; }

    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        => context.IsFirstPass ? OnExecuteInternal(context) : Suspend();

    protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        => OnExecuteInternal(context);

    private IActivityExecutionResult OnExecuteInternal(ActivityExecutionContext context)
    {
        var input = context.GetInput<PropertyChangedEvent>();
        Output = input;
        return Done(Output);
    }

    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DevicePropertyManager.PropertyNames;
}

