using AppBroker.Elsa.Activities;

using Elsa.Services;


namespace AppBroker.Elsa.Bookmarks
{
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
                && (string.IsNullOrWhiteSpace(PropertyName) || string.Equals(PropertyName, other.PropertyName))
                && (string.IsNullOrWhiteSpace(DeviceName) || string.Equals(DeviceName, other.DeviceName))
                && (string.IsNullOrWhiteSpace(TypeName) || string.Equals(TypeName, other.TypeName))
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
            var propertyName = await context.ReadActivityPropertyAsync(a => a.PropertyName);
            var deviceId = await context.ReadActivityPropertyAsync(a => a.DeviceId);
            var deviceName = await context.ReadActivityPropertyAsync(a => a.DeviceName);
            var typeName = await context.ReadActivityPropertyAsync(a => a.TypeName);
            var result = Result(new DeviceChangedEventBookmark(propertyName, deviceName, deviceId, typeName));
            return new[] { result };
        }
    }
}