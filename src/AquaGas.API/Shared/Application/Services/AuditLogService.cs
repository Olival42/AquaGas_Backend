namespace AquaGas.API.Shared.Application.Services;

using System.Text.Json;
using AquaGas.API.Shared.Application.Repositories;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.API.Shared.Domain.Models;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IHttpContextAccessor _http;

    public AuditLogService(
        IAuditLogRepository repository,
        IHttpContextAccessor http)
    {
        _repository = repository;
        _http = http;
    }

    public async Task LogAsync(
        Guid? userId,
        string? userName,
        AuditAction action,
        string entityType,
        Guid? entityId,
        object? oldValues = null,
        object? newValues = null)
    {
        var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _http.HttpContext?.Request?.Headers["User-Agent"].ToString();

        var log = AuditLog.Create(
            userId,
            userName,
            action,
            entityType,
            entityId,
            oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ip,
            userAgent,
            Guid.NewGuid()
        );

        await _repository.AddAsync(log);
    }
}