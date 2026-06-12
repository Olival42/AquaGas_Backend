using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Plan.Infrastructure.Repositories;

public sealed class ContractPenaltyRepository
    : IContractPenaltyRepository
{
    private readonly PlanDbContext _context;

    public ContractPenaltyRepository(
        PlanDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ContractPenalty contractPenalty)
    {
        await _context.ContractPenalties
            .AddAsync(contractPenalty);
    }

    public void Update(
        ContractPenalty contractPenalty)
    {
        _context.ContractPenalties
            .Update(contractPenalty);
    }

    public async Task<ContractPenalty?> GetByIdAsync(
        Guid id)
    {
        return await _context.ContractPenalties
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<ContractPenalty>> GetByPlanIdAsync(
        Guid planId)
    {
        return await _context.ContractPenalties
            .Where(x => x.PlanId == planId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ContractPenalty>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.ContractPenalties
            .Join(_context.Plans,
                penalty => penalty.PlanId,
                plan => plan.Id,
                (penalty, plan) => new { penalty, plan })
            .Where(x => x.plan.CustomerId == customerId)
            .Select(x => x.penalty)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ContractPenalty>> GetReportAsync(
        DateTime? start,
        DateTime? end)
    {
        var query =
            _context.ContractPenalties
                .AsNoTracking()
                .Join(
                    _context.Plans.AsNoTracking(),
                    penalty => penalty.PlanId,
                    plan => plan.Id,
                    (penalty, plan) => new
                    {
                        Penalty = penalty,
                        Plan = plan
                    })
                .AsQueryable();

        if (start.HasValue)
            query = query.Where(x => x.Penalty.Timestamp >= start.Value);

        if (end.HasValue)
            query = query.Where(x => x.Penalty.Timestamp <= end.Value);

        return await query
            .OrderByDescending(x => x.Penalty.Timestamp)
            .Select(x => x.Penalty)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
