using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Infrastructure.Repositories;

public sealed class PlanRepository
    : IPlanRepository
{
    private readonly PlanDbContext _context;

    public PlanRepository(
        PlanDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        PlanEntity plan)
    {
        await _context.Plans.AddAsync(plan);
    }

    public void Update(
        PlanEntity plan)
    {
        _context.Plans.Update(plan);
    }

    public async Task<PlanEntity?> GetByIdAsync(
        Guid id)
    {
        return await _context.Plans
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<PlanEntity>> GetByCustomerIdAsync(
        Guid customerId)
    {
        return await _context.Plans
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }

    public async Task<List<PlanEntity>> GetAllAsync()
    {
        return await _context.Plans
            .Include(x => x.Items)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
