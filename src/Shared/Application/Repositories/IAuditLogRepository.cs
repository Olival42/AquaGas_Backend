using AquaGas.Shared.Domain.Models;

namespace AquaGas.Shared.Application.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}