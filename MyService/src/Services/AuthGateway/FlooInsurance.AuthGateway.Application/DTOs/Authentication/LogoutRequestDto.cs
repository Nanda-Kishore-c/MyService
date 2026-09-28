namespace FlooInsurance.AuthGateway.Application.DTOs.Authentication;

public class LogoutRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
