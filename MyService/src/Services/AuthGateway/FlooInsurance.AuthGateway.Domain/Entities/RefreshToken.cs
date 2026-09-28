using FlooInsurance.AuthGateway.Domain.Exceptions;

namespace FlooInsurance.AuthGateway.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Required by EF Core
    protected RefreshToken() { }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        string? createdByIp = null,
        Guid? id = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("TokenHash cannot be empty.");

        if (expiresAt <= createdAt)
            throw new DomainException("ExpiresAt must be after CreatedAt.");

        Id = id ?? Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash.Trim();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public void Revoke(string? revokedByIp, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
            return; // Already revoked

        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public bool IsActiveAt(DateTime utcNow) => RevokedAt == null && utcNow < ExpiresAt;
    public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresAt;
}
