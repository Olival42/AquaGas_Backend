using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AquaGas.Plan.Infrastructure.Configurations;

public sealed class BillingConfiguration
    : IEntityTypeConfiguration<Billing>
{
    public void Configure(
        EntityTypeBuilder<Billing> builder)
    {
        builder.ToTable("Billings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.Period)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.OwnsOne(x => x.Amount, amount =>
        {
            amount.Property(x => x.Value)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.PaidAt);

        builder.Property(x => x.ReceivedBy);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<PlanEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}