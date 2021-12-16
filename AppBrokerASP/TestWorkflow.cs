
using AppBrokerASP.Devices.Zigbee;

using Elsa.Activities.Console;
using Elsa.Activities.File.Models;
using Elsa.Activities.File;
using Elsa.Builders;

using System.Text;
using AppBroker.Elsa.Models;
using AppBroker.Elsa.Signaler;

namespace AppBrokerASP
{
    public class SignalerCreatorWorkflow : IWorkflow
    {
        public SignalerCreatorWorkflow(WorkflowDeviceSignaler a, WorkflowPropertySignaler b)
        {

        }

        public void Build(IWorkflowBuilder builder) { }
    }

    //public class TestWorkflow : IWorkflow
    //{

    //    public void Build(IWorkflowBuilder builder)
    //    {


    //        _ = builder.Then<DeviceChangedTrigger>(setup: (setup) =>
    //          {

    //            //.Set(x => x.DeviceId, 6066005697233659)
    //            //.Set(x => x.DeviceName, "Sascha Zimmer");

    //        })
    //        .WriteLine(setup: (setup) =>
    //        {
    //            _ = setup.WithText(async context =>
    //              {
    //                  var device = await context.WorkflowExecutionContext.GetActivityPropertyAsync<DeviceChangedTrigger, DeviceChangedEvent>("activity-1", a => a.Output!);
    //                  var line = $"{GetType().Name}-{device!.DeviceName}-{device.DeviceId}-{device.TypeName}-{device.ToJson()}";
    //                  return line;
    //              });
    //        });

    //    }
    //}

    //public class TestWorkflow2 : IWorkflow
    //{

    //    public void Build(IWorkflowBuilder builder)
    //    {
    //        _ = builder.Then<PropertyChangedTrigger>(setup: (setup) =>
    //          {
    //              setup.Set(x => x.PropertyName, "Temperature");
    //          })
    //        .WriteLine(setup: (setup) =>
    //        {
    //            _ = setup.WithText(async context =>
    //              {
    //                  PropertyChangedEvent? device = await context.WorkflowExecutionContext.GetActivityPropertyAsync<PropertyChangedTrigger, PropertyChangedEvent>("activity-1", a => a.Output!);
    //                  var line = $"{GetType().Name}-{device!.DeviceName}-{device.DeviceId}-{device.TypeName}-{device.ToJson()}";
    //                  return line;
    //              });
    //        });

    //    }
    //}
}
