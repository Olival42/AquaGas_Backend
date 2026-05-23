using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AquaGas.Plan.Infrastructure.Persistence;

public sealed class PlanDbContextFactory
    : IDesignTimeDbContextFactory<PlanDbContext>
{
    public PlanDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<PlanDbContext>();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        optionsBuilder.UseNpgsql(connectionString);

        return new PlanDbContext(
            optionsBuilder.Options);
    }
}