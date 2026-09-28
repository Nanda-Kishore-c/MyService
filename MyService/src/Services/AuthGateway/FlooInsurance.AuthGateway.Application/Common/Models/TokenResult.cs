namespace FlooInsurance.AuthGateway.Application.Common.Models;

public sealed record TokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
