using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.CurrentUser;

public class GetCurrentUserUseCase : IGetCurrentUserUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public GetCurrentUserUseCase(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<CurrentUserResponseDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new AuthenticationException("User is not authenticated.");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new AuthenticationException("User account not found or inactive.");
        }

        var roles = await _roleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);

        return new CurrentUserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = roles
        };
    }
}
