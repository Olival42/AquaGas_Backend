using AquaGas.Plan.Domain.Models;

namespace AquaGas.Plan.Domain.Repositories;

public interface IDeliveryRepository
{
    Task AddAsync(Delivery delivery);

    void Update(Delivery delivery);

    Task<Delivery?> GetByIdAsync(Guid id);

    Task<List<Delivery>> GetByPlanIdAsync(Guid planId);

    Task<List<Delivery>> GetDeliveredByPeriodAsync(
        DateTime start,
        DateTime end);

    Task<List<Delivery>> GetPendingAsync();

    Task SaveChangesAsync();
}
