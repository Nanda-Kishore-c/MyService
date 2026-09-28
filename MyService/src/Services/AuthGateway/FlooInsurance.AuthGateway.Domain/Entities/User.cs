using FlooInsurance.AuthGateway.Domain.Exceptions;
using FlooInsurance.AuthGateway.Domain.ValueObjects;

namespace FlooInsurance.AuthGateway.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Required by EF Core
    protected User() { }

    public User(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        bool isActive = true,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty.");

        var validatedEmail = ValueObjects.Email.Create(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        Id = id ?? Guid.NewGuid();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = validatedEmail.Value;
        PasswordHash = passwordHash;
        IsActive = isActive;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_userRoles.Any(ur => ur.RoleId == role.Id || string.Equals(ur.Role?.Name, role.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return; // Role already assigned
        }

        _userRoles.Add(new UserRole(this, role));
        UpdatedAt = DateTime.UtcNow;
    }

    public bool RemoveRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        var userRole = _userRoles.FirstOrDefault(ur =>
            string.Equals(ur.Role?.Name, roleName, StringComparison.OrdinalIgnoreCase));

        if (userRole != null)
        {
            _userRoles.Remove(userRole);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public bool HasRole(string roleName)
    {
        return _userRoles.Any(ur =>
            string.Equals(ur.Role?.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }

    public void AddRefreshToken(RefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        _refreshTokens.Add(refreshToken);
    }
}
