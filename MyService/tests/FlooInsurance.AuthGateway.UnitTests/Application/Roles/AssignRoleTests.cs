using FluentAssertions;
using FluentValidation;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Roles;
using FlooInsurance.AuthGateway.Application.Features.Roles.AssignRole;
using FlooInsurance.AuthGateway.Application.Validators;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Roles;

public class AssignRoleTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IValidator<AssignRoleRequestDto> _validator;
    private readonly AssignRoleUseCase _useCase;

    public AssignRoleTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validator = new AssignRoleRequestValidator();

        _useCase = new AssignRoleUseCase(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validator);
    }

    [Fact]
    public async Task AssignRole_WithValidRoleAndUser_AssignsRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequestDto { Role = RoleConstants.ClaimManager };

        var user = new User("Claim", "Manager", "claim@example.com", "hash", id: userId);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roleId = Guid.NewGuid();
        var role = new Role(roleId, RoleConstants.ClaimManager);
        _roleRepositoryMock
            .Setup(r => r.GetByNameAsync(RoleConstants.ClaimManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(r => r.GetUserRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole?)null);

        // Act
        await _useCase.ExecuteAsync(userId, request);

        // Assert
        _roleRepositoryMock.Verify(r => r.AddUserRoleAsync(
            It.Is<UserRole>(ur => ur.UserId == userId && ur.RoleId == roleId),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRole_WithInvalidRoleName_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequestDto { Role = "SuperHackerAdmin" };

        // Act
        var act = async () => await _useCase.ExecuteAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AssignRole_WhenUserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequestDto { Role = RoleConstants.Admin };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(userId, request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
