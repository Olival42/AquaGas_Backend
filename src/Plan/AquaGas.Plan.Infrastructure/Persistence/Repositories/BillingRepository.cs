using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Plan.Infrastructure.Repositories;

public sealed class BillingRepository
    : IBillingRepository
{
    private readonly PlanDbContext _context;

    public BillingRepository(
        PlanDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Billing billing)
    {
        await _context.Billings.AddAsync(billing);
    }

    public void Update(Billing billing)
    {
        _context.Billings.Update(billing);
    }

    public async Task<Billing?> GetByIdAsync(Guid id)
    {
        return await _context.Billings
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Billing>> GetByPlanIdAsync(Guid planId)
    {
        return await _context.Billings
            .Where(x => x.PlanId == planId)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task<List<Billing>> GetByPlanIdsAsync(
        IEnumerable<Guid> planIds)
    {
        var planIdList = planIds.Distinct().ToList();

        if (planIdList.Count == 0)
            return [];

        return await _context.Billings
            .AsNoTracking()
            .Where(x => planIdList.Contains(x.PlanId))
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task<List<Billing>> GetPendingAsync()
    {
        return await _context.Billings
            .Where(x => x.Status == Domain.Enums.BillingStatus.Pending)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task<List<Billing>> GetLateAsync()
    {
        return await _context.Billings
            .Where(x => x.Status == Domain.Enums.BillingStatus.Late)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task<List<Billing>> GetLateByCustomerIdAsync(Guid customerId)
    {
        return await _context.Billings
            .Join(_context.Plans,
                billing => billing.PlanId,
                plan => plan.Id,
                (billing, plan) => new { billing, plan })
            .Where(x => x.plan.CustomerId == customerId && 
                       (x.billing.Status == Domain.Enums.BillingStatus.Late || 
                       (x.billing.Status == Domain.Enums.BillingStatus.Pending && DateTime.UtcNow > x.billing.DueDate)))
            .Select(x => x.billing)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
