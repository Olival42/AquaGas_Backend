using AquaGas.Auth.Domain.Models;

namespace AquaGas.Auth.Application.Services;

public interface IAuthService
{
    Task<User?> Authenticate(string userName, string password);
}