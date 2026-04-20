using AquaGas.Api.Modules.Auth.Domain.Models;

namespace AquaGas.Api.Modules.Auth.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(Guid id);
}