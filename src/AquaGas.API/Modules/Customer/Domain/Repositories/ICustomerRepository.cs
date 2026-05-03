using CustomerEntity = AquaGas.Api.Modules.Customer.Domain.Models.Customer;
using AddressEntity = AquaGas.Api.Modules.Customer.Domain.Models.Address;

namespace AquaGas.Api.Modules.Customer.Domain.Repositories;

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