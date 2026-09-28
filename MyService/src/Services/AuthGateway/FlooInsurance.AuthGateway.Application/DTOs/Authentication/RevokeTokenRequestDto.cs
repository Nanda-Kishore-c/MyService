namespace FlooInsurance.AuthGateway.Application.DTOs.Authentication;

public class RevokeTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
