namespace FlooInsurance.AuthGateway.Infrastructure.Configuration;

public class AdminSeedSettings
{
    public const string SectionName = "AdminSeedSettings";

    public bool SeedAdminUser { get; set; } = false;
    public string Email { get; set; } = "admin@flooinsurance.com";
    public string Password { get; set; } = "AdminSecure2026!";
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Administrator";
}
