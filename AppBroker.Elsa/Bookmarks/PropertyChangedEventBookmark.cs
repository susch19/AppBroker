
using AppBroker.Activities;

using Elsa.Attributes;
using Elsa.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppBroker.Elsa.Bookmarks;

public class PropertyChangedEventBookmark : IBookmark
{
    public PropertyChangedEventBookmark()
    { }

    public PropertyChangedEventBookmark(string propertyName)//, object oldValue, object newValue)
    {
        PropertyName = propertyName;
    }

    [global::Elsa.Attributes.ExcludeFromHash]
    public string? PropertyName { get; set; }

    public bool? Compare(IBookmark bookmark)
    {
        return bookmark is PropertyChangedEventBookmark other
            && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal)
            //&& OldValue == other.OldValue
            //&& NewValue == other.NewValue;
            ;
    }

    public static bool operator ==(PropertyChangedEventBookmark? left, PropertyChangedEventBookmark? right)
    {
        return EqualityComparer<PropertyChangedEventBookmark>.Default.Equals(left, right);
    }

    public static bool operator !=(PropertyChangedEventBookmark? left, PropertyChangedEventBookmark? right)
    {
        return !(left == right);
    }
}

public class PropertyChangedEventBookmarkProvider : BookmarkProvider<PropertyChangedEventBookmark, PropertyChangedTrigger>
{
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<PropertyChangedTrigger> context, CancellationToken cancellationToken)
    {
        var propertyName = await context.ReadActivityPropertyAsync(a => a.PropertyName);
        var result = Result(new PropertyChangedEventBookmark(propertyName));
        return new[] { result };
    }
}
