using AquaGas.Api.Modules.Auth.Domain.Models;

namespace AquaGas.Api.Modules.Auth.Domain.Repositories;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> AnyByUserNameAsync(string userName);
    Task<bool> AnyByUserNameAsync(string userName, Guid ignoreUserId);
}