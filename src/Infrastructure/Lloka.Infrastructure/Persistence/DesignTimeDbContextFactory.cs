using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lloka.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LlokaDbContext>
{
    public LlokaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LlokaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=lloka_dev;Username=lloka;Password=lloka");
        return new LlokaDbContext(optionsBuilder.Options);
    }
}
