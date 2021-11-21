using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBroker.Elsa;

[Feature("Property")]
public class Startup : StartupBase
{
    public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration) => _ = elsa.AddPropetyActivities();
}
