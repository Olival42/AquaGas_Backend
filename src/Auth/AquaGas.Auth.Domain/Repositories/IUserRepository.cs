using AquaGas.Auth.Domain.Models;

namespace AquaGas.Auth.Domain.Repositories;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> AnyByUserNameAsync(string userName);
    Task<bool> AnyByUserNameAsync(string userName, Guid ignoreUserId);
    Task<User?> GetByEmployeeIdAsync(Guid employeeId);
    Task<User?> GetByEmployeeIdAsync(Guid employeeId, bool onlyActive = true);
    Task SaveChangesAsync();
    void Update(User user);
}