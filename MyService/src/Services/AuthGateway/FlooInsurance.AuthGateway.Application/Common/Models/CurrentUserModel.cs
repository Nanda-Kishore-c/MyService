namespace FlooInsurance.AuthGateway.Application.Common.Models;

public sealed record CurrentUserModel(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
