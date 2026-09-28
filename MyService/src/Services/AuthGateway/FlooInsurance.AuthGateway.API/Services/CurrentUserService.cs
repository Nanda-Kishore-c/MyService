using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;

namespace FlooInsurance.AuthGateway.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
        ?? HttpContext?.User.FindFirst("email")?.Value;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (HttpContext?.User == null)
                return Array.Empty<string>();

            return HttpContext.User.FindAll(ClaimTypes.Role)
                .Concat(HttpContext.User.FindAll("role"))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool IsAuthenticated => HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? IpAddress
    {
        get
        {
            var context = HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                    return ip;
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
