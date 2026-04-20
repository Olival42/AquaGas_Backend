namespace AquaGas.Api.Modules.Auth.Application.Services;

using AquaGas.Api.Modules.Auth.Domain.Models;

public interface IUserService
{
    Task<User?> GetById(Guid id);
}