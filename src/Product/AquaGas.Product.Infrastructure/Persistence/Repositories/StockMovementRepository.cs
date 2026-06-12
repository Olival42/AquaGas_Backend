using AquaGas.Product.Domain.Models;
using AquaGas.Product.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Product.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly ProductDbContext _context;

    public StockMovementRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockMovement log)
    {
        await _context.StockMovements.AddAsync(log);
    }

    public async Task<List<StockMovement>> GetReportAsync(
        DateTime start,
        DateTime end)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
