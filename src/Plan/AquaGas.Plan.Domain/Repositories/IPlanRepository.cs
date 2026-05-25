using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Domain.Repositories;

public interface IPlanRepository
{
    Task AddAsync(PlanEntity plan);

    void Update(PlanEntity plan);

    Task<PlanEntity?> GetByIdAsync(Guid id);

    Task<List<PlanEntity>> GetByIdsAsync(IEnumerable<Guid> ids);

    Task<Dictionary<Guid, PlanEntity>> GetByDeliveryReferenceIdsAsync(
        IEnumerable<Guid> deliveryReferenceIds);

    Task<List<PlanEntity>> GetAllAsync();

    Task SaveChangesAsync();
}
