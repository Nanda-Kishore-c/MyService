using FluentAssertions;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.Features.Authentication.CurrentUser;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Authentication;

public class CurrentUserTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly GetCurrentUserUseCase _useCase;

    public CurrentUserTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();

        _useCase = new GetCurrentUserUseCase(
            _currentUserServiceMock.Object,
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsCurrentUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);

        var user = new User("Jane", "Doe", "jane@example.com", "hash", id: userId);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roles = new[] { RoleConstants.Customer };
        _roleRepositoryMock
            .Setup(r => r.GetRolesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _useCase.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("jane@example.com");
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.Roles.Should().Contain(RoleConstants.Customer);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUnauthenticated_ThrowsAuthenticationException()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync();

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
