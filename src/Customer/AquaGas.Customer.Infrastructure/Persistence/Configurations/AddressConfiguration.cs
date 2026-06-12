using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AquaGas.Customer.Domain.Models;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Street)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Neighborhood)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Number)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.Complement)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(a => a.Cep, zip =>
        {
            zip.Property(z => z.Value)
                .HasColumnName("Cep")
                .HasMaxLength(8)
                .IsRequired();
        });
    }
}