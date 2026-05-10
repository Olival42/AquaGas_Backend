using Microsoft.EntityFrameworkCore;

namespace AquaGas.Employee.Infrastructure.Persistence;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;

public class EmployeeDbContext : DbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EmployeeDbContext).Assembly
        );
    }
}