using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Extension;

public static class Extensions
{
    public static List<int> IndexesOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
    {
        List<int> values = new();
        int oldi = -1;
        int i = 0;
        while (true)
        {
            if (oldi > span.Length)
                return values;
            i = span[(oldi + 1)..].IndexOf(value);
            if (i >= 0)
                values.Add(i + oldi + 1);
            else
                return values;
            oldi += i + 1;
        }
    }

    public static List<int> IndexesOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
    {
        List<int> values = new();
        int oldi = -1;
        int i = 0;
        while (true)
        {
            if (oldi > span.Length)
                return values;
            i = span[(oldi + 1)..].IndexOf(value);
            if (i >= 0)
                values.Add(i + oldi + 1);
            else
                return values;
            oldi += i + 1;
        }
    }

    public static List<int> IndexesOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
    {
        List<int> values = new();
        int oldi = -1;
        int i = 0;
        while (true)
        {
            if (oldi > span.Length)
                return values;
            i = span[(oldi + 1)..].IndexOf(value);
            if (i >= 0)
                values.Add(i + oldi + 1);
            else
                return values;
            oldi += i + 1;
        }
    }

    public static List<int> IndexesOf<T>(this Span<T> span, T value) where T : IEquatable<T>
    {
        List<int> values = new();
        int oldi = -1;
        int i = 0;
        while (true)
        {
            if (oldi > span.Length)
                return values;
            i = span[(oldi + 1)..].IndexOf(value);
            if (i >= 0)
                values.Add(i + oldi + 1);
            else
                return values;
            oldi += i + 1;
        }
    }

    public static async Task<SocketIOResponse> Emit(this SocketIO socket, string eventName, params object[] data)
    {
        var task = new TaskCompletionSource<SocketIOResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        await socket.EmitAsync(eventName, response =>
        {
            try
            {
                task.SetResult(response);
            }
            catch (Exception ex)
            {
                task.SetException(ex);
            }
        }, data);

        // Ensure follwoing tasks will run async
        await Task.Yield();

        return await task.Task;
    }
}
