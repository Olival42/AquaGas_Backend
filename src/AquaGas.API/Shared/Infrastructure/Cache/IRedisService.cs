namespace AquaGas.Api.Shared.Infrastructure.Cache;

public interface IRedisService
{
    Task SetAsync(string key, string value, TimeSpan expiry);
    Task<string?> GetAsync(string key);
}
