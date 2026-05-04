using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Domain.ValueObjects;
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

    public async Task<EmployeeEntity?> GetByIdAsync(Guid id, bool onlyActive = true)
    {
        return await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e =>
                e.Id == id
                && (!onlyActive || e.IsActive));
    }

    public async Task AddAsync(EmployeeEntity employee)
    {
        await _context.Employees.AddAsync(employee);
    }

    public async Task<EmployeeEntity?> GetByCPFAsync(string cpf)
    {
        return await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.CPF.Value == cpf);
    }

    public async Task<bool> AnyByCPFAsync(string cpf, bool onlyActive = true)
    {
        return await _context.Employees
            .AnyAsync(e => e.CPF.Value == cpf && (!onlyActive || e.IsActive));
    }

    public async Task<bool> AnyByEmailAsync(string email, bool onlyActive = true)
    {
        return await _context.Employees
            .AnyAsync(e => e.Email.Value == email && (!onlyActive || e.IsActive));
    }

    public async Task<bool> AnyByEmailAsync(string email, Guid ignoreEmployeeId)
    {
        var parsed = Email.Create(email);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Employees
            .AnyAsync(u =>
                u.Email.Value == match.Value &&
                u.Id != ignoreEmployeeId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EmployeeEntity>> GetAllAsync(bool onlyActive = true)
    {
        return await _context.Employees
            .Include(e => e.User)
            .AsNoTracking()
            .Where(c => !onlyActive || c.IsActive)
            .ToListAsync();
    }

    public void Update(EmployeeEntity employee)
    {
        _context.Employees.Update(employee);
    }
}