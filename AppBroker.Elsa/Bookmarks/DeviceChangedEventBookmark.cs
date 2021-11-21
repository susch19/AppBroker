using AppBroker.Elsa.Activities;

using Elsa.Services;

using System;
using System.Collections.Generic;

namespace AppBroker.Elsa.Bookmarks;

public class DeviceChangedEventBookmark : IBookmark
{
    public DeviceChangedEventBookmark()
    { }

    public DeviceChangedEventBookmark(string propertyName, string deviceName, long? deviceId, string typeName)
    {
        PropertyName = propertyName;
        DeviceName = deviceName;
        TypeName = typeName;
        DeviceId = deviceId;
    }

    [global::Elsa.Attributes.ExcludeFromHash]
    public string? PropertyName { get; set; }

    [global::Elsa.Attributes.ExcludeFromHash]
    public string DeviceName { get; set; }

    [global::Elsa.Attributes.ExcludeFromHash]
    public string TypeName { get; set; }

    [global::Elsa.Attributes.ExcludeFromHash]
    public long? DeviceId { get; set; }

    public bool? Compare(IBookmark bookmark)
    {
        return bookmark is DeviceChangedEventBookmark other
            && (string.IsNullOrWhiteSpace(PropertyName) || string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal))
            && (string.IsNullOrWhiteSpace(DeviceName) || string.Equals(DeviceName, other.DeviceName, StringComparison.Ordinal))
            && (string.IsNullOrWhiteSpace(TypeName) || string.Equals(TypeName, other.TypeName, StringComparison.Ordinal))
            && (DeviceId is null || DeviceId == other.DeviceId);
    }

    public static bool operator ==(DeviceChangedEventBookmark? left, DeviceChangedEventBookmark? right)
    {
        return EqualityComparer<DeviceChangedEventBookmark>.Default.Equals(left, right);
    }

    public static bool operator !=(DeviceChangedEventBookmark? left, DeviceChangedEventBookmark? right)
    {
        return !(left == right);
    }
}

public class DeviceChangedEventBookmarkProvider : BookmarkProvider<DeviceChangedEventBookmark, DeviceChangedTrigger>
{
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<DeviceChangedTrigger> context, CancellationToken cancellationToken)
    {
        var propertyName = await context.ReadActivityPropertyAsync(a => a.PropertyName, cancellationToken);
        var deviceId = await context.ReadActivityPropertyAsync(a => a.DeviceId, cancellationToken);
        var deviceName = await context.ReadActivityPropertyAsync(a => a.DeviceName, cancellationToken);
        var typeName = await context.ReadActivityPropertyAsync(a => a.TypeName, cancellationToken);
        var result = Result(new DeviceChangedEventBookmark(propertyName, deviceName, deviceId, typeName));
        return new[] { result };
    }
}
