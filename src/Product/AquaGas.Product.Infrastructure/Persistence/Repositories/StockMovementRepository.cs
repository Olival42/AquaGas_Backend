using AquaGas.Product.Domain.Models;
using AquaGas.Shared.Infrastructure.Persistence;
using AquaGas.Product.Application.Repositories;

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
}
