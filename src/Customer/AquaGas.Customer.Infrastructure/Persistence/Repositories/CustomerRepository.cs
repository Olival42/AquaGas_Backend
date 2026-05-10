using AquaGas.Customer.Domain.Repositories;
using AquaGas.Customer.Domain.ValueObjects.Customer;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;

namespace AquaGas.Customer.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _context;

    public CustomerRepository(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerEntity?> GetByIdAsync(Guid id, bool onlyActive = true)
    {
        return await _context.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (!onlyActive || c.IsActive));
    }

    public async Task<CustomerEntity?> GetByDocumentAsync(string document)
    {
        return await _context.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Document.Value == document);
    }

    public async Task AddAsync(CustomerEntity Customer)
    {
        await _context.Customers.AddAsync(Customer);
    }

    public async Task<bool> AnyByDocumentAsync(string document)
    {
        return await _context.Customers
            .AnyAsync(c => c.Document.Value == document);
    }

    public async Task<bool> AnyByDocumentAsync(string document, Guid ignoreCostumerId)
    {
        var parsed = Document.Create(document);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Customers
            .AnyAsync(u =>
                u.Document.Value == match.Value &&
                u.Id != ignoreCostumerId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<CustomerEntity>> GetAllAsync(bool onlyActive = true)
    {
        return await _context.Customers
            .Include(c => c.Addresses)
            .AsNoTracking()
            .Where(c => !onlyActive || c.IsActive)
            .ToListAsync();
    }

    public void Update(CustomerEntity Customer)
    {
        _context.Customers.Update(Customer);
    }
}