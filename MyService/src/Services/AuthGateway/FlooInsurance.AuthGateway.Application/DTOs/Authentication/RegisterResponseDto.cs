namespace FlooInsurance.AuthGateway.Application.DTOs.Authentication;

public class RegisterResponseDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
