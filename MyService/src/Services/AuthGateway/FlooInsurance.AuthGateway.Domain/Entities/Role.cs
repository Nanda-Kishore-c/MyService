using FlooInsurance.AuthGateway.Domain.Exceptions;

namespace FlooInsurance.AuthGateway.Domain.Entities;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // Required by EF Core
    protected Role() { }

    public Role(Guid id, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        Description = description?.Trim();
    }
}
