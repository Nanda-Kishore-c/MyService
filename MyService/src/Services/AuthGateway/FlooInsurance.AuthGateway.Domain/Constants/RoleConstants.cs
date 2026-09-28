namespace FlooInsurance.AuthGateway.Domain.Constants;

public static class RoleConstants
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string ClaimManager = "ClaimManager";

    public static readonly IReadOnlyList<string> AllRoles = new[]
    {
        Customer,
        Admin,
        ClaimManager
    };

    public static bool IsValidRole(string roleName)
    {
        return AllRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
}
