using Microsoft.AspNetCore.SignalR;

namespace AppBrokerASP.Extension;


public interface IServiceExtender
{
    void UseEndpoints(IEndpointRouteBuilder configure) { }
    void ConfigureServices(IServiceCollection serviceCollection) { }
    IEnumerable<Type> GetHubTypes() { yield break; }
}
