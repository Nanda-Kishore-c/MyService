using FluentAssertions;
using FluentValidation;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Login;
using FlooInsurance.AuthGateway.Application.Validators;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Authentication;

public class LoginTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IValidator<LoginRequestDto> _validator;
    private readonly LoginUserUseCase _useCase;

    public LoginTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validator = new LoginRequestValidator();

        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _useCase = new LoginUserUseCase(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            _unitOfWorkMock.Object,
            _validator);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndRoles()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var user = new User("John", "Doe", "john.doe@example.com", "valid_hash");
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("john.doe@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        var roles = new[] { RoleConstants.Customer };
        _roleRepositoryMock
            .Setup(r => r.GetRolesByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var expiresAt = new DateTime(2026, 1, 1, 12, 15, 0, DateTimeKind.Utc);
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(user, roles))
            .Returns("jwt.access.token");
        _tokenServiceMock
            .Setup(t => t.GetAccessTokenExpiration())
            .Returns(expiresAt);

        _refreshTokenServiceMock
            .Setup(r => r.GenerateRefreshToken())
            .Returns("raw_refresh_token_123");
        _refreshTokenServiceMock
            .Setup(r => r.HashToken("raw_refresh_token_123"))
            .Returns("hashed_refresh_token_123");
        _refreshTokenServiceMock
            .Setup(r => r.GetRefreshTokenExpiration())
            .Returns(new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _useCase.ExecuteAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt.access.token");
        result.RefreshToken.Should().Be("raw_refresh_token_123");
        result.ExpiresAt.Should().Be(expiresAt);
        result.User.Email.Should().Be("john.doe@example.com");
        result.User.Roles.Should().Contain(RoleConstants.Customer);

        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt =>
            rt.UserId == user.Id &&
            rt.TokenHash == "hashed_refresh_token_123" &&
            rt.CreatedByIp == "127.0.0.1"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("nonexistent@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "john@example.com",
            Password = "WrongPassword!"
        };

        var user = new User("John", "Doe", "john@example.com", "valid_hash");
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "inactive@example.com",
            Password = "Password123!"
        };

        var user = new User("Inactive", "User", "inactive@example.com", "valid_hash", isActive: false);
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("inactive@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*inactive*");
    }
}
