using AquaGas.Plan.Domain.Models;
using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AquaGas.Plan.Infrastructure.Configurations;

public sealed class PlanItemConfiguration
    : IEntityTypeConfiguration<PlanItem>
{
    public void Configure(
        EntityTypeBuilder<PlanItem> builder)
    {
        builder.ToTable("PlanItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.OwnsOne(x => x.Quantity, quantity =>
        {
            quantity.Property(x => x.Value)
                .HasColumnName("Quantity")
                .IsRequired();
        });

        builder.HasOne<PlanEntity>()
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}