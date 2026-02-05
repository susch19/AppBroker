using NonSucking.Framework.Extension.EntityFrameworkCore;

namespace AppBroker.Plugins.Database;


public class BaseDbContext : DatabaseContext
{
    public string DatabaseType { get; protected set; }

    public BaseDbContext()
    {
        EnableUseLazyLoading = true;
        AssemblyRootName = nameof(AppBroker);
        AddAllEntities = false;
    }

}