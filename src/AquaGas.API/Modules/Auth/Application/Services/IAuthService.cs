namespace AquaGas.Api.Modules.Auth.Application.Services;

using AquaGas.Api.Modules.Auth.Domain.Models;

public interface IAuthService
{
    Task<User?> Authenticate(string userName, string password);
}