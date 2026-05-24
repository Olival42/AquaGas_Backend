using SaleEntity = AquaGas.Sale.Domain.Models.Sale;
using Microsoft.EntityFrameworkCore;
using AquaGas.Sale.Domain.Repositories;

namespace AquaGas.Sale.Infrastructure.Persistence.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly SaleDbContext _context;

    public SaleRepository(SaleDbContext context)
    {
        _context = context;
    }

    public async Task<SaleEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Sales
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<SaleEntity?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Sales
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(SaleEntity sale)
    {
        await _context.Sales.AddAsync(sale);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SaleEntity>> GetAllAsync()
    {
        return await _context.Sales
            .Include(x => x.Items)
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<SaleEntity>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Sales
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<SaleEntity>> GetByCustomerIdWithItemsAsync(Guid customerId)
    {
        return await _context.Sales
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<SaleEntity>> GetByPeriodAsync(
        DateTime? startDate,
        DateTime? endDate)
    {
        var query =
            _context.Sales
                .Include(x => x.Items)
                .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(x =>
                x.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x =>
                x.Date <= endDate.Value);
        }

        return await query.ToListAsync();
    }

    public void Update(SaleEntity sale)
    {
        _context.Sales.Update(sale);
    }
}
