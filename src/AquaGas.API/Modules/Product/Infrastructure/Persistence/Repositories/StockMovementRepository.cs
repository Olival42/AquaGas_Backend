using AquaGas.Api.Modules.Product.Domain.Models;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using AquaGas.API.Modules.Product.Application.Repositories;

namespace AquaGas.Api.Modules.Product.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockMovement log)
    {
        await _context.StockMovements.AddAsync(log);
    }
}
