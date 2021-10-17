using AppBroker.Elsa.Activities;

using Elsa.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppBroker.Elsa.Bookmarks
{
    public class PropertyChangedEventBookmark : IBookmark
    {
        public PropertyChangedEventBookmark()
        { }

        public PropertyChangedEventBookmark(string propertyName)//, object oldValue, object newValue)
        {
            PropertyName = propertyName;
            //OldValue = oldValue;
            //NewValue = newValue;
        }

        public string? PropertyName { get; set; }
        //public object OldValue { get; }
        //public object NewValue { get; }

        public bool IsSame(IBookmark bookmark)
        {
            return bookmark is PropertyChangedEventBookmark other
                && string.Equals(PropertyName, other.PropertyName)
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
}