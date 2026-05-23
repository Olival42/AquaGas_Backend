using PlanEntity = AquaGas.Plan.Domain.Models.Plan;

namespace AquaGas.Plan.Domain.Repositories;

public interface IPlanRepository
{
    Task AddAsync(PlanEntity plan);

    void Update(PlanEntity plan);

    Task<PlanEntity?> GetByIdAsync(Guid id);

    Task<List<PlanEntity>> GetAllAsync();

    Task SaveChangesAsync();
}