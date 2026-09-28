using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FlooInsurance.AuthGateway.Application.DTOs.Common;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Infrastructure.Configuration;

namespace FlooInsurance.AuthGateway.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in strict production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    // Suppress default challenge response to return consistent ErrorResponseDto
                    context.HandleResponse();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var traceId = context.HttpContext.TraceIdentifier;
                    var errorResponse = new ErrorResponseDto(
                        StatusCodes.Status401Unauthorized,
                        "Authentication failed: You are not authorized or the token is invalid/expired.",
                        traceId);

                    var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(json);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var traceId = context.HttpContext.TraceIdentifier;
                    var errorResponse = new ErrorResponseDto(
                        StatusCodes.Status403Forbidden,
                        "Access forbidden: You do not possess the required role permissions to perform this action.",
                        traceId);

                    var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(json);
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(RoleConstants.Admin));
            options.AddPolicy("RequireCustomerRole", policy => policy.RequireRole(RoleConstants.Customer));
            options.AddPolicy("RequireClaimManagerRole", policy => policy.RequireRole(RoleConstants.ClaimManager));
        });

        return services;
    }
}
