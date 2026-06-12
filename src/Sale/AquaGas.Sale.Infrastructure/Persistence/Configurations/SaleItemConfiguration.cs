namespace AquaGas.Sale.Infrastructure.Persistence.Configurations;

using AquaGas.Sale.Domain.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SaleItemConfiguration
    : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.SaleId)
            .HasColumnName("sale_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.OwnsOne(x => x.Quantity, quantity =>
        {
            quantity.Property(x => x.Value)
                .HasColumnName("quantity")
                .IsRequired();
        });

        builder.OwnsOne(x => x.TotalPrice, totalPrice =>
        {
            totalPrice.Property(x => x.Value)
                .HasColumnName("total_price")
                .HasPrecision(18, 2)
                .IsRequired();
        });
    }
}