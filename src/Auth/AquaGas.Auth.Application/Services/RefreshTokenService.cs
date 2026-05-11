using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Shared.Security;

namespace AquaGas.Auth.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;

    public RefreshTokenService(IRefreshTokenRepository repository)
    {
        _repository = repository;
    }

    public async Task StoreAsync(User user, string refreshToken, DateTime expiresAt)
    {
        var hash = TokenHasher.Hash(refreshToken);

        var entity = new RefreshToken(
            user.Id,
            hash,
            expiresAt
        );

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetAsync(string refreshToken)
    {
        var hashedToken = TokenHasher.Hash(refreshToken);
        return await _repository.GetByTokenHashAsync(hashedToken);
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        await _repository.UpdateAsync(refreshToken);
        await _repository.SaveChangesAsync();
    }
}