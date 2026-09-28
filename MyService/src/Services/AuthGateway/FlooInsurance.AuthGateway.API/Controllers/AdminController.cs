using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlooInsurance.AuthGateway.Application.DTOs.Common;
using FlooInsurance.AuthGateway.Application.DTOs.Roles;
using FlooInsurance.AuthGateway.Application.Features.Roles.AssignRole;
using FlooInsurance.AuthGateway.Application.Features.Roles.GetRoles;
using FlooInsurance.AuthGateway.Application.Features.Roles.RemoveRole;
using FlooInsurance.AuthGateway.Domain.Constants;

namespace FlooInsurance.AuthGateway.API.Controllers;

[Route("api/auth/admin")]
[Authorize(Roles = RoleConstants.Admin)]
public class AdminController : BaseApiController
{
    private readonly IAssignRoleUseCase _assignRoleUseCase;
    private readonly IRemoveRoleUseCase _removeRoleUseCase;
    private readonly IGetUserRolesUseCase _getUserRolesUseCase;

    public AdminController(
        IAssignRoleUseCase assignRoleUseCase,
        IRemoveRoleUseCase removeRoleUseCase,
        IGetUserRolesUseCase getUserRolesUseCase)
    {
        _assignRoleUseCase = assignRoleUseCase;
        _removeRoleUseCase = removeRoleUseCase;
        _getUserRolesUseCase = getUserRolesUseCase;
    }

    /// <summary>
    /// Assign a system role to a user. Restricted to Administrators.
    /// </summary>
    [HttpPost("users/{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] Guid userId,
        [FromBody] AssignRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        await _assignRoleUseCase.ExecuteAsync(userId, request, cancellationToken);
        return Ok(new { message = $"Role '{request.Role}' assigned successfully to user {userId}." });
    }

    /// <summary>
    /// Remove a system role from a user. Restricted to Administrators.
    /// </summary>
    [HttpDelete("users/{userId:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(
        [FromRoute] Guid userId,
        [FromRoute] string role,
        CancellationToken cancellationToken)
    {
        await _removeRoleUseCase.ExecuteAsync(userId, role, cancellationToken);
        return Ok(new { message = $"Role '{role}' removed successfully from user {userId}." });
    }

    /// <summary>
    /// Retrieve roles assigned to a user. Restricted to Administrators.
    /// </summary>
    [HttpGet("users/{userId:guid}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRoles(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var roles = await _getUserRolesUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(roles);
    }
}
