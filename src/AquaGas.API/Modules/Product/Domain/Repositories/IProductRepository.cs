using ProductEntity = AquaGas.Api.Modules.Product.Domain.Models.Product;

namespace AquaGas.Api.Modules.Product.Domain.Repositories;

public interface IProductRepository
{
    Task<ProductEntity?> GetByIdAsync(Guid id, bool onlyActive = true);
    Task<ProductEntity?> GetByNameAsync(string name);
    Task AddAsync(ProductEntity product);
    Task<bool> AnyByNameAsync(string name, bool onlyActive = true);
    Task<bool> AnyByNameAsync(string name, Guid ignoreProductId);
    Task SaveChangesAsync();
    Task<IEnumerable<ProductEntity>> GetAllAsync(bool onlyActive = true);
    void Update(ProductEntity product);
}