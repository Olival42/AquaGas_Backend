using Microsoft.EntityFrameworkCore;

namespace AquaGas.Customer.Infrastructure.Persistence;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CustomerDbContext).Assembly
        );
    }
}