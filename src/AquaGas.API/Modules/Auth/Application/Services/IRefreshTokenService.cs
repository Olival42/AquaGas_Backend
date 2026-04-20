namespace AquaGas.Api.Modules.Auth.Application.Services;

using AquaGas.Api.Modules.Auth.Domain.Models;

public interface IRefreshTokenService
{
    Task StoreAsync(User user, string refreshToken, DateTime expiresAt);
    Task<RefreshToken?> GetAsync(string refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
}