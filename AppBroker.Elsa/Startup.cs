using Elsa.Attributes;
using Elsa.Options;
using Elsa.Services.Startup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Runtime.Versioning;

namespace AppBroker.Elsa;

[Feature("Property")]
[RequiresPreviewFeatures]
public class Startup : StartupBase
{
    public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration) => _ = elsa.AddPropertyActivities();
}
