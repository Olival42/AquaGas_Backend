namespace AquaGas.Api.Modules.Auth.Domain.Models;

public class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public RefreshToken(Guid userId, string tokenHash, DateTime expirationDate)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpirationDate = expirationDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetRevoked()
    {
        if (IsRevoked) return;

        RevokedAt = DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpirationDate;
    }
}