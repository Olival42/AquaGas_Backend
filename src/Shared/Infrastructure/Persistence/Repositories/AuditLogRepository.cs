namespace AquaGas.Shared.Infrastructure.Persistence.Repositories;

using AquaGas.Shared.Infrastructure.Persistence;
using AquaGas.Shared.Application.Repositories;
using AquaGas.Shared.Domain.Models;

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
