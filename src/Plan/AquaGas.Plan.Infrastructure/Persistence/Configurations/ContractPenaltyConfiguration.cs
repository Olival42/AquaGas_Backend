using AquaGas.Plan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Infrastructure.Configurations;

public sealed class ContractPenaltyConfiguration
    : IEntityTypeConfiguration<ContractPenalty>
{
    public void Configure(
        EntityTypeBuilder<ContractPenalty> builder)
    {
        builder.ToTable("ContractPenalties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.InitiatedBy)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.OwnsOne(x => x.OriginalValue, price =>
        {
            price.Property(x => x.Value)
                .HasColumnName("OriginalValue")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(x => x.RemainingValue, price =>
        {
            price.Property(x => x.Value)
                .HasColumnName("RemainingValue")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(x => x.CalculatedAmount, price =>
        {
            price.Property(x => x.Value)
                .HasColumnName("CalculatedAmount")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.PaidDate);

        builder.Property(x => x.PaidBy);

        builder.Property(x => x.WaivedBy);

        builder.Property(x => x.WaivedAt);

        builder.Property(x => x.WaiveReason)
            .HasMaxLength(500);

        builder.Property(x => x.CanceledBy);

        builder.Property(x => x.CanceledAt);

        builder.Property(x => x.CancelReason)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne<PlanEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}