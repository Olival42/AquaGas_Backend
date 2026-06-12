using AquaGas.Plan.Domain.Models;
using AquaGas.Plan.Domain.Enums;

namespace AquaGas.Plan.Domain.Repositories;

public interface IContractPenaltyRepository
{
    Task AddAsync(ContractPenalty contractPenalty);

    void Update(ContractPenalty contractPenalty);

    Task<ContractPenalty?> GetByIdAsync(Guid id);

    Task<List<ContractPenalty>> GetByPlanIdAsync(Guid planId);

    Task<List<ContractPenalty>> GetByCustomerIdAsync(Guid customerId);

    Task<List<ContractPenalty>> GetReportAsync(
        DateTime? start,
        DateTime? end);

    Task SaveChangesAsync();
}
