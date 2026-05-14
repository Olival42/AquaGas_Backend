namespace AquaGas.Sale.Infrastructure.Persistence.Configurations;

using AquaGas.Sale.Domain.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired(false);

        builder.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(x => x.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.OwnsOne(x => x.Total, total =>
        {
            total.Property(x => x.Value)
                .HasColumnName("total")
                .HasPrecision(18, 2)
                .IsRequired();
        });

        builder.OwnsOne(x => x.CurrentDiscount, discount =>
        {
            discount.Property(x => x.Value)
                .HasColumnName("current_discount");
        });

        builder.Navigation(x => x.CurrentDiscount)
            .IsRequired(false);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}