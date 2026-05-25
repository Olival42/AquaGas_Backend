using AquaGas.Plan.Domain.Enums;
using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Repositories;
using AquaGas.Plan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Plan.Infrastructure.Repositories;

public sealed class DeliveryRepository
    : IDeliveryRepository
{
    private readonly PlanDbContext _context;

    public DeliveryRepository(
        PlanDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Delivery delivery)
    {
        await _context.Deliveries
            .AddAsync(delivery);
    }

    public void Update(
        Delivery delivery)
    {
        _context.Deliveries
            .Update(delivery);
    }

    public async Task<Delivery?> GetByIdAsync(
        Guid id)
    {
        return await _context.Deliveries
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Delivery>> GetByPlanIdAsync(
        Guid planId)
    {
        return await _context.Deliveries
            .Where(x => x.PlanId == planId)
            .OrderBy(x => x.Period)
            .ToListAsync();
    }

    public async Task<List<Delivery>> GetDeliveredByPeriodAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Deliveries
            .AsNoTracking()
            .Where(x =>
                x.Status == DeliveryStatus.Delivered &&
                x.DeliveryDate.HasValue &&
                x.DeliveryDate.Value >= start &&
                x.DeliveryDate.Value <= end)
            .OrderByDescending(x => x.DeliveryDate)
    public async Task<List<Delivery>> GetByPlanIdsAsync(
        IEnumerable<Guid> planIds)
    {
        var planIdList = planIds.Distinct().ToList();

        if (planIdList.Count == 0)
            return [];

        return await _context.Deliveries
            .AsNoTracking()
            .Where(x => planIdList.Contains(x.PlanId))
            .OrderByDescending(x => x.DeliveryDate ?? x.DueDate)
            .ToListAsync();
    }

    public async Task<List<Delivery>> GetPendingAsync()
    {
        return await _context.Deliveries
            .Where(x =>
                x.Status == DeliveryStatus.Pending)
            .OrderBy(x => x.DueDate)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
