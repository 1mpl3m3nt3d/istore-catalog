using Catalog.Host.Configurations;

namespace Catalog.Host.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private readonly IOptionsMonitor<DatabaseConfig> _options;

    public ApplicationDbContextFactory(IOptionsMonitor<DatabaseConfig> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        return new ApplicationDbContext(dbContextOptionsBuilder.Options, _options);
    }
}
