namespace FlooInsurance.AuthGateway.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    string HashToken(string rawToken);
    DateTime GetRefreshTokenExpiration();
}
