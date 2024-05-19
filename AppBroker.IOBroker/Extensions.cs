using SocketIOClient;

namespace AppBroker.IOBroker;
public static class Extensions
{

    public static async Task<SocketIOResponse> Emit(this  SocketIOClient.SocketIO socket, string eventName, params object[] data)
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
