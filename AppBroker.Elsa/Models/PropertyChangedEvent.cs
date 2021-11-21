using System;
using System.Collections.Generic;
using System.Text;

namespace AppBroker.Elsa.Models;

public class PropertyChangedEvent
{
    public string PropertyName { get; set; }
}
public class PropertyChangedEvent<T> : PropertyChangedEvent
{
    public T OldValue { get; set; }
    public T NewValue { get; set; }

    public PropertyChangedEvent()
    {

    }

    public PropertyChangedEvent(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
