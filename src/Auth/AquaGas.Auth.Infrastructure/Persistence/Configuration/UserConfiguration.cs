using AquaGas.Auth.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.HasIndex(x => x.EmployeeId)
            .IsUnique();

        builder.OwnsOne(x => x.UserName, username =>
        {
            username.Property(x => x.Value)
                .HasColumnName("UserName")
                .IsRequired();

            username.HasIndex(x => x.Value)
                .IsUnique();
        });

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.Role)
            .IsRequired();
    }
}