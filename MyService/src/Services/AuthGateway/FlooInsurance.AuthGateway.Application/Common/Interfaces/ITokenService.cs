using FlooInsurance.AuthGateway.Domain.Entities;

namespace FlooInsurance.AuthGateway.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    DateTime GetAccessTokenExpiration();
}
