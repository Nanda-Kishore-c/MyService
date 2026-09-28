using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Application.DTOs.Common;
using FlooInsurance.AuthGateway.Application.Features.Authentication.CurrentUser;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Login;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Logout;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Refresh;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Register;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Revoke;

namespace FlooInsurance.AuthGateway.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IRegisterUserUseCase _registerUserUseCase;
    private readonly ILoginUserUseCase _loginUserUseCase;
    private readonly IRefreshTokenUseCase _refreshTokenUseCase;
    private readonly ILogoutUseCase _logoutUseCase;
    private readonly IRevokeTokenUseCase _revokeTokenUseCase;
    private readonly IGetCurrentUserUseCase _getCurrentUserUseCase;

    public AuthController(
        IRegisterUserUseCase registerUserUseCase,
        ILoginUserUseCase loginUserUseCase,
        IRefreshTokenUseCase refreshTokenUseCase,
        ILogoutUseCase logoutUseCase,
        IRevokeTokenUseCase revokeTokenUseCase,
        IGetCurrentUserUseCase getCurrentUserUseCase)
    {
        _registerUserUseCase = registerUserUseCase;
        _loginUserUseCase = loginUserUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _logoutUseCase = logoutUseCase;
        _revokeTokenUseCase = revokeTokenUseCase;
        _getCurrentUserUseCase = getCurrentUserUseCase;
    }

    /// <summary>
    /// Register a new customer user account.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _registerUserUseCase.ExecuteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Authenticate a user with email and password, issuing access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var response = await _loginUserUseCase.ExecuteAsync(request, ipAddress, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Rotate and refresh an expired or valid access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var response = await _refreshTokenUseCase.ExecuteAsync(request, ipAddress, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Log out the user by revoking the specified refresh token session.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        await _logoutUseCase.ExecuteAsync(request, ipAddress, cancellationToken);
        return Ok(new { message = "Logged out successfully. Session invalidated." });
    }

    /// <summary>
    /// Explicitly revoke a refresh token.
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        await _revokeTokenUseCase.ExecuteAsync(request, ipAddress, cancellationToken);
        return Ok(new { message = "Token revoked successfully." });
    }

    /// <summary>
    /// Retrieve the currently authenticated user's profile and roles.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var response = await _getCurrentUserUseCase.ExecuteAsync(cancellationToken);
        return Ok(response);
    }
}
