using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Repositories;

namespace AquaGas.Api.Modules.Auth.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> GetById(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}