using AquaGas.Product.Domain.Repositories;
using AquaGas.Shared.Infrastructure.Persistence;
using AquaGas.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using ProductEntity = AquaGas.Product.Domain.Models.Product;

namespace AquaGas.Product.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<ProductEntity?> GetByIdAsync(Guid id, bool onlyActive = true)
    {
        return await _context.Products
            .FirstOrDefaultAsync(e =>
                e.Id == id
                && (!onlyActive || e.IsActive));
    }

    public async Task<ProductEntity?> GetByNameAsync(string name)
    {
        var normalized = StringNormalizer.Normalize(name);

        return await _context.Products
            .FirstOrDefaultAsync(p => p.NormalizedName == normalized);
    }

    public async Task AddAsync(ProductEntity product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task<bool> AnyByNameAsync(string name, bool onlyActive = true)
    {
        var normalized = StringNormalizer.Normalize(name);

        return await _context.Products
            .Where(p => !onlyActive || p.IsActive)
            .AnyAsync(p =>
                p.NormalizedName == normalized
            );
    }

    public async Task<bool> AnyByNameAsync(string name, Guid ignoreProductId)
    {
        var normalized = StringNormalizer.Normalize(name);

        return await _context.Products
            .Where(p => p.Id != ignoreProductId && p.IsActive)
            .AnyAsync(p =>
                p.NormalizedName == normalized
            );
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductEntity>> GetAllAsync(bool onlyActive = true)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(c => !onlyActive || c.IsActive)
            .ToListAsync();
    }

    public void Update(ProductEntity product)
    {
        _context.Products.Update(product);
    }
}