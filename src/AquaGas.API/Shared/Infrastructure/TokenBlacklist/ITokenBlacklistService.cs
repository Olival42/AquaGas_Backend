namespace AquaGas.Api.Shared.Infrastructure.TokenBlacklist;

public interface ITokenBlacklistService
{
    Task AddAsync(string token, DateTime expiresAt);
    Task<bool> IsBlacklistedAsync(string token);
}
