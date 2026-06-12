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

    public async Task<List<PlanEntity>> GetByIdsAsync(
        IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();

        if (idList.Count == 0)
            return [];

        return await _context.Plans
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => idList.Contains(x.Id))
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, PlanEntity>> GetByDeliveryReferenceIdsAsync(
        IEnumerable<Guid> deliveryReferenceIds)
    {
        var deliveryIdList = deliveryReferenceIds.Distinct().ToList();

        if (deliveryIdList.Count == 0)
            return [];

        return await _context.Deliveries
            .AsNoTracking()
            .Where(d => deliveryIdList.Contains(d.Id))
            .Join(
                _context.Plans.AsNoTracking().Include(p => p.Items),
                delivery => delivery.PlanId,
                plan => plan.Id,
                (delivery, plan) => new
                {
                    DeliveryId = delivery.Id,
                    Plan = plan
                })
            .ToDictionaryAsync(x => x.DeliveryId, x => x.Plan);
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
