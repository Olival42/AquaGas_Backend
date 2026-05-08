using AquaGas.Api.Shared.Infrastructure.TokenBlacklist;

namespace AquaGas.IntegrationTests.Helpers;

public sealed class NoOpTokenBlacklistService : ITokenBlacklistService
{
    public Task AddAsync(string token, DateTime expiresAt) => Task.CompletedTask;

    public Task<bool> IsBlacklistedAsync(string token) => Task.FromResult(false);
}
