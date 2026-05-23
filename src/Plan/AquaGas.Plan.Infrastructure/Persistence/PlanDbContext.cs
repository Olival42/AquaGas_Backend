namespace AquaGas.Plan.Infrastructure.Persistence;

using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;

public sealed class PlanDbContext : DbContext
{
    public PlanDbContext(
        DbContextOptions<PlanDbContext> options)
        : base(options)
    {
    }

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanItem> PlanItems => Set<PlanItem>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<Billing> Billings => Set<Billing>();

    public DbSet<ContractPenalty> ContractPenalties
        => Set<ContractPenalty>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new PlanConfiguration());

        modelBuilder.ApplyConfiguration(
            new PlanItemConfiguration());

        modelBuilder.ApplyConfiguration(
            new DeliveryConfiguration());

        modelBuilder.ApplyConfiguration(
            new BillingConfiguration());

        modelBuilder.ApplyConfiguration(
            new ContractPenaltyConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}