using AquaGas.API.Shared.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AquaGas.API.Shared.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired(false);

        builder.Property(x => x.UserName)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .IsRequired(false);

        builder.Property(x => x.OldValues)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.NewValues)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EntityType);
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.CorrelationId);
    }
}