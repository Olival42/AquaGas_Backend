namespace AquaGas.API.Shared.Application.Repositories;

using AquaGas.API.Shared.Domain.Models;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}