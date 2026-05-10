using AquaGas.Shared.Domain.Enums;

namespace AquaGas.Shared.Domain.Models;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }

    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = null!;
    public Guid? EntityId { get; private set; }

    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
    Guid? userId,
    string? userName,
    AuditAction action,
    string entityType,
    Guid? entityId,
    string? oldValues,
    string? newValues,
    string? ipAddress,
    string? userAgent,
    Guid correlationId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };
    }
}