using AquaGas.Api.Modules.Product.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;

namespace AquaGas.Api.Modules.Product.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(sm => sm.Id);

        builder.Property(sm => sm.Id)
            .ValueGeneratedNever();

        builder.Property(sm => sm.ProductId)
            .IsRequired();

        builder.Property(sm => sm.Type)
            .HasConversion<string>() 
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sm => sm.Quantity)
            .IsRequired();

        builder.Property(sm => sm.Reason)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(sm => sm.ReferenceId);

        builder.Property(sm => sm.CreatedBy)
            .IsRequired();

        builder.Property(sm => sm.CreatedAt)
            .IsRequired();

        builder.HasOne(sm => sm.Product)
            .WithMany()
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sm => sm.ProductId);
        builder.HasIndex(sm => sm.Type);
        builder.HasIndex(sm => sm.CreatedAt);
    }
}