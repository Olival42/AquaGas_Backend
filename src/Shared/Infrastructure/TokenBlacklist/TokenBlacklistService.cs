using AquaGas.Shared.Infrastructure.Cache;

namespace AquaGas.Shared.Infrastructure.TokenBlacklist;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IRedisService _redis;

    public TokenBlacklistService(IRedisService redis)
    {
        _redis = redis;
    }

    public async Task AddAsync(string token, DateTime expiresAt)
    {
        var ttl = expiresAt - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
            return;

        await _redis.SetAsync(
            $"blacklist:access:{token}",
            "revoked",
            ttl
        );
    }

    public async Task<bool> IsBlacklistedAsync(string token)
    {
        var value = await _redis.GetAsync($"blacklist:access:{token}");
        return value != null;
    }
}
