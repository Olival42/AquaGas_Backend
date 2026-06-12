using AquaGas.Plan.Domain.Models;

namespace AquaGas.Plan.Domain.Repositories;

public interface IBillingRepository
{
    Task AddAsync(Billing billing);

    void Update(Billing billing);

    Task<Billing?> GetByIdAsync(Guid id);

    Task<List<Billing>> GetByPlanIdAsync(Guid planId);

    Task<List<Billing>> GetByPlanIdsAsync(IEnumerable<Guid> planIds);

    Task<List<Billing>> GetPendingAsync();

    Task<List<Billing>> GetLateAsync();

    Task<List<Billing>> GetLateByCustomerIdAsync(Guid customerId);

    Task SaveChangesAsync();
}
