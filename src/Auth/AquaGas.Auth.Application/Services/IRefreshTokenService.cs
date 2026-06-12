namespace AquaGas.Auth.Application.Services;

using AquaGas.Auth.Domain.Models;

public interface IRefreshTokenService
{
    Task StoreAsync(User user, string refreshToken, DateTime expiresAt);
    Task<RefreshToken?> GetAsync(string refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
}