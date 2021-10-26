using System;
using System.Collections.Generic;
using System.Text;

namespace AppBroker.Elsa.Models
{
    public class DeviceChangedEvent
    {
        public string PropertyName { get; set; }
        public string DeviceName { get; set; }
        public string TypeName { get; set; }
        public long DeviceId { get; set; }
    }
    public class DeviceChangedEvent<TDevice, TValue> : DeviceChangedEvent
    {
        public TValue OldValue { get; set; }
        public TValue NewValue { get; set; }
        public TDevice Device { get; set; }

        public DeviceChangedEvent()
        {

        }

        public DeviceChangedEvent(TDevice device, TValue oldValue, TValue newValue)
        {
            Device = device;
            OldValue = oldValue;
            NewValue = newValue;
        }

    }
}
