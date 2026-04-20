namespace AquaGas.API.Shared.Application.Services;

using AquaGas.API.Shared.Domain.Enums;

public interface IAuditLogService
{
    Task LogAsync(
        Guid? userId,
        string? userName,
        AuditAction action,
        string entityType,
        Guid? entityId,
        object? oldValues = null,
        object? newValues = null);
}