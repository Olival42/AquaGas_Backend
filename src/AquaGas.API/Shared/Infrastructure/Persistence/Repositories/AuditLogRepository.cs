namespace AquaGas.Api.Shared.Infrastructure.Persistence.Repositories;

using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.API.Shared.Application.Repositories;
using AquaGas.API.Shared.Domain.Models;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
