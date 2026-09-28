namespace FlooInsurance.AuthGateway.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }

    // Required by EF Core
    protected UserRole() { }

    public UserRole(Guid userId, Guid roleId, DateTime? assignedAt = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = assignedAt ?? DateTime.UtcNow;
    }

    public UserRole(User user, Role role, DateTime? assignedAt = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(role);

        UserId = user.Id;
        User = user;
        RoleId = role.Id;
        Role = role;
        AssignedAt = assignedAt ?? DateTime.UtcNow;
    }
}
