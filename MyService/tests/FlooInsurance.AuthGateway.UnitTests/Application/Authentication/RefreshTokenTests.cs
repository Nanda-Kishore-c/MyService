using FluentAssertions;
using FluentValidation;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Refresh;
using FlooInsurance.AuthGateway.Application.Validators;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Authentication;

public class RefreshTokenTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IValidator<RefreshTokenRequestDto> _validator;
    private readonly RefreshTokenUseCase _useCase;

    public RefreshTokenTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validator = new RefreshTokenRequestValidator();

        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _useCase = new RefreshTokenUseCase(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _refreshTokenServiceMock.Object,
            _tokenServiceMock.Object,
            _dateTimeProviderMock.Object,
            _unitOfWorkMock.Object,
            _validator);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokenAndReturnsNewPair()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "valid_raw_refresh_token" };
        var hashedOldToken = "hashed_old_token";

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns(hashedOldToken);

        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var oldToken = new RefreshToken(userId, hashedOldToken, now.AddDays(-1), now.AddDays(6));

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(hashedOldToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);

        var user = new User("John", "Doe", "john@example.com", "hash", id: userId);
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roles = new[] { RoleConstants.Customer };
        _roleRepositoryMock
            .Setup(r => r.GetRolesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _refreshTokenServiceMock
            .Setup(r => r.GenerateRefreshToken())
            .Returns("new_raw_refresh_token");
        _refreshTokenServiceMock
            .Setup(r => r.HashToken("new_raw_refresh_token"))
            .Returns("hashed_new_token");
        _refreshTokenServiceMock
            .Setup(r => r.GetRefreshTokenExpiration())
            .Returns(now.AddDays(7));

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(user, roles))
            .Returns("new_jwt_access_token");
        _tokenServiceMock
            .Setup(t => t.GetAccessTokenExpiration())
            .Returns(now.AddMinutes(15));

        // Act
        var result = await _useCase.ExecuteAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_jwt_access_token");
        result.RefreshToken.Should().Be("new_raw_refresh_token");

        // Verify old token was revoked with reference to new token hash
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenHash.Should().Be("hashed_new_token");

        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(oldToken, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(rt =>
            rt.UserId == userId &&
            rt.TokenHash == "hashed_new_token"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_TriggersReuseDetectionAndRevokesAllSessions()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "revoked_raw_refresh_token" };
        var hashedToken = "hashed_revoked_token";

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns(hashedToken);

        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var revokedToken = new RefreshToken(userId, hashedToken, now.AddDays(-2), now.AddDays(5));
        revokedToken.Revoke("127.0.0.1", "compromised_token");

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*reuse detected*");

        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(userId, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "expired_raw_refresh_token" };
        var hashedToken = "hashed_expired_token";

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns(hashedToken);

        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        // Expired yesterday
        var expiredToken = new RefreshToken(userId, hashedToken, now.AddDays(-10), now.AddDays(-1));

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task RefreshToken_WithNonExistentToken_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new RefreshTokenRequestDto { RefreshToken = "nonexistent_token" };

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns("hashed_nonexistent_token");

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync("hashed_nonexistent_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("Invalid refresh token.");
    }
}
