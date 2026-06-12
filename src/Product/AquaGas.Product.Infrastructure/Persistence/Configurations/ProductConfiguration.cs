using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductEntity = AquaGas.Product.Domain.Models.Product;

namespace AquaGas.Product.Infrastructure.Persistence.Configurations;

public class ProductConfiguration
    : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(p => p.Value)
                .HasColumnName("price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.OwnsOne(x => x.Quantity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("quantity")
                .IsRequired();
        });
    }
}