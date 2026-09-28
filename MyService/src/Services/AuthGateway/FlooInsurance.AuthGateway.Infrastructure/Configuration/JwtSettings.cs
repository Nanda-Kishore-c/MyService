using System.ComponentModel.DataAnnotations;

namespace FlooInsurance.AuthGateway.Infrastructure.Configuration;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT SecretKey is required.")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters (256 bits).")]
    public string SecretKey { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "AccessTokenExpirationMinutes must be between 1 and 1440.")]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 365, ErrorMessage = "RefreshTokenExpirationDays must be between 1 and 365.")]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
