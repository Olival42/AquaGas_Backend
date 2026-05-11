namespace AquaGas.Auth.Domain.Repositories;

using AquaGas.Auth.Domain.Models;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    Task SaveChangesAsync();
    
    Task UpdateAsync(RefreshToken refreshToken);

    Task RevokeAllByUserId(Guid userId);
}