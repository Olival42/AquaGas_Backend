using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AquaGas.Plan.Infrastructure.Configurations;

public sealed class PlanConfiguration
    : IEntityTypeConfiguration<PlanEntity>
{
    public void Configure(
        EntityTypeBuilder<PlanEntity> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.Cycle)
            .HasConversion<string>()
            .IsRequired();

        builder.OwnsOne(x => x.Total, total =>
        {
            total.Property(x => x.Value)
                .HasColumnName("Total")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(x => x.CurrentDiscount, discount =>
        {
            discount.Property(x => x.Value)
                .HasColumnName("CurrentDiscount")
                .HasPrecision(5, 2);
        });

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.FinishedAt)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("PlanId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
