using AquaGas.Plan.Domain.Models;

namespace AquaGas.Plan.Domain.Repositories;

public interface IContractPenaltyRepository
{
    Task AddAsync(ContractPenalty contractPenalty);

    void Update(ContractPenalty contractPenalty);

    Task<ContractPenalty?> GetByIdAsync(Guid id);

    Task<List<ContractPenalty>> GetByPlanIdAsync(Guid planId);

    Task<List<ContractPenalty>> GetByCustomerIdAsync(Guid customerId);

    Task SaveChangesAsync();
}