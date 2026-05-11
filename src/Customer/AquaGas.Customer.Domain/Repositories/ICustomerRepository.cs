using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;

namespace AquaGas.Customer.Domain.Repositories;

public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByIdAsync(Guid id, bool onlyActive = true);
    Task<CustomerEntity?> GetByDocumentAsync(string document);
    Task AddAsync(CustomerEntity customer);
    Task<bool> AnyByDocumentAsync(string document);
    Task<bool> AnyByDocumentAsync(string document, Guid ignoreCostumerId);
    Task SaveChangesAsync();
    Task<IEnumerable<CustomerEntity>> GetAllAsync(bool onlyActive = true);
    void Update(CustomerEntity customer);
}