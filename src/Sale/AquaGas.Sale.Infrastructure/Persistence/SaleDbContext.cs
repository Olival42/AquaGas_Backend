namespace AquaGas.Sale.Infrastructure.Persistence;

using AquaGas.Sale.Domain.Models;
using AquaGas.Sale.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;

public sealed class SaleDbContext : DbContext
{
    public SaleDbContext(DbContextOptions<SaleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SaleConfiguration());

        modelBuilder.ApplyConfiguration(new SaleItemConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}