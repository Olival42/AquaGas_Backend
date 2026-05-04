using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;

namespace AquaGas.Api.Modules.Employee.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<EmployeeEntity?> GetByIdAsync(Guid id, bool onlyActive = true);
    Task AddAsync(EmployeeEntity employee);
    Task<EmployeeEntity?> GetByCPFAsync(string cpf);
    Task<bool> AnyByCPFAsync(string cpf, bool onlyActive = true);
    Task<bool> AnyByEmailAsync(string email, bool onlyActive = true);
    Task<bool> AnyByEmailAsync(string email, Guid ignoreEmployeeId);
    Task SaveChangesAsync();
    Task<IEnumerable<EmployeeEntity>> GetAllAsync(bool onlyActive = true);
    void Update(EmployeeEntity employee);
}