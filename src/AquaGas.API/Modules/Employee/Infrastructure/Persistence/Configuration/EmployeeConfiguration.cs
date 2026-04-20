using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AquaGas.Api.Modules.Employee.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Models;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(p => p.Value)
                .HasColumnName("Name")
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.OwnsOne(x => x.CPF, cpf =>
        {
            cpf.Property(p => p.Value)
                .HasColumnName("CPF")
                .HasMaxLength(11)
                .IsRequired();

            cpf.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(p => p.Value)
                .HasColumnName("Email")
                .HasMaxLength(200)
                .IsRequired();

            email.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(x => x.Phone, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("Phone")
                .HasMaxLength(11)
                .IsRequired();
        });

        builder
            .HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}