using FluentAssertions;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.Features.Roles.RemoveRole;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Roles;

public class RemoveRoleTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveRoleUseCase _useCase;

    public RemoveRoleTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _useCase = new RemoveRoleUseCase(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task RemoveRole_WithValidData_RemovesRole()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(currentAdminId);

        var user = new User("Target", "User", "target@example.com", "hash", id: targetUserId);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roleId = Guid.NewGuid();
        var role = new Role(roleId, RoleConstants.ClaimManager);
        _roleRepositoryMock
            .Setup(r => r.GetByNameAsync(RoleConstants.ClaimManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var userRole = new UserRole(targetUserId, roleId);
        _roleRepositoryMock
            .Setup(r => r.GetUserRoleAsync(targetUserId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);

        // Act
        await _useCase.ExecuteAsync(targetUserId, RoleConstants.ClaimManager);

        // Assert
        _roleRepositoryMock.Verify(r => r.RemoveUserRoleAsync(userRole, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_WhenAdminAttemptsToRemoveOwnAdminRole_ThrowsValidationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(adminId);

        // Act
        var act = async () => await _useCase.ExecuteAsync(adminId, RoleConstants.Admin);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*cannot remove their own Admin role*");
    }
}
