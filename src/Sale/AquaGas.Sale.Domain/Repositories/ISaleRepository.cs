using SaleEntity = AquaGas.Sale.Domain.Models.Sale;

namespace AquaGas.Sale.Domain.Repositories;

public interface ISaleRepository
{
    Task<SaleEntity?> GetByIdAsync(Guid id);
    Task<SaleEntity?> GetByIdWithItemsAsync(Guid id);
    Task<IEnumerable<SaleEntity>> GetByCustomerIdWithItemsAsync(Guid customerId);
    Task AddAsync(SaleEntity sale);
    Task SaveChangesAsync();
    Task<IEnumerable<SaleEntity>> GetAllAsync();
    Task<IEnumerable<SaleEntity>> GetByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<SaleEntity>> GetByPeriodAsync(
        DateTime? startDate,
        DateTime? endDate
    );
    void Update(SaleEntity sale);
}
