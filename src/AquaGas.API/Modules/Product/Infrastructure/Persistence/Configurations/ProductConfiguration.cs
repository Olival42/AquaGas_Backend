using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;

namespace AquaGas.Api.Modules.Product.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.OwnsOne(p => p.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(150)
                .IsRequired();

            name.HasIndex(n => n.Value).IsUnique();
        });

        builder.Property(p => p.NormalizedName)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(p => p.NormalizedName)
            .IsUnique();

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(p => p.Value)
                .HasColumnName("Price")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
        });

        builder.OwnsOne(p => p.Quantity, quantity =>
        {
            quantity.Property(q => q.Value)
                .HasColumnName("Quantity")
                .IsRequired();
        });

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();
    }
}