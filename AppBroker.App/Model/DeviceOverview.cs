using System.ComponentModel.DataAnnotations;

namespace AppBroker.App.Model;

public record struct DeviceOverview(long Id, string FriendlyName, string TypeName, List<string> TypeNames);
