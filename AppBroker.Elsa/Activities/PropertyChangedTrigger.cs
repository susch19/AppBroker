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

namespace AppBroker.Elsa.Activities
{

    [Trigger(
       Category = "Smarthome",
       Description = "Waits for an event sent from your application."
   )]
    public class PropertyChangedTrigger : Activity
    {
        [ActivityInput(
            Label = "Property Name",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string PropertyName { get; set; } = default!;

        [ActivityInput(Label = "Device Name", SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string DeviceName { get; set; } = default!;

        [ActivityInput(Label = "Device Id", SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public long DeviceId { get; set; } = default!;


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
    }
}
