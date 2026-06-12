using AquaGas.Product.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Product.Infrastructure.Persistence;
using ProductEntity = AquaGas.Product.Domain.Models.Product;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProductDbContext).Assembly
        );
    }
}