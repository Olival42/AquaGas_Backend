using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;

namespace AquaGas.API.Modules.Employee.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e =>
                e.Id == id
                && e.IsActive);
    }

    public async Task AddAsync(EmployeeEntity employee)
    {
        await _context.Employees.AddAsync(employee);
    }

    public async Task<bool> AnyByCPFAsync(string cpf)
    {
        return await _context.Employees
            .AnyAsync(e => e.CPF.Value == cpf && e.IsActive);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EmployeeEntity>> GetAllAsync()
    {
        return await _context.Employees
            .Where(e => e.IsActive)
            .Include(e => e.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public void Update(EmployeeEntity employee)
    {
        _context.Employees.Update(employee);
    }

    public async Task<bool> AnyByEmailAsync(string email)
    {
        return await _context.Employees
            .AnyAsync(e => e.Email.Value == email && e.IsActive);
    }
}