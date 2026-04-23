using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;

namespace AquaGas.Api.Modules.Employee.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<EmployeeEntity?> GetByIdAsync(Guid id);
    Task AddAsync(EmployeeEntity employee);
    Task<bool> AnyByCPFAsync(string cpf);
    Task<bool> AnyByEmailAsync(string email);
    Task SaveChangesAsync();
    Task<IEnumerable<EmployeeEntity>> GetAllAsync();
    void Update(EmployeeEntity employee);
}