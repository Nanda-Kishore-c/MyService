using FluentAssertions;
using FluentValidation;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Logout;
using FlooInsurance.AuthGateway.Application.Validators;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Authentication;

public class LogoutTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IValidator<LogoutRequestDto> _validator;
    private readonly LogoutUseCase _useCase;

    public LogoutTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validator = new LogoutRequestValidator();

        _useCase = new LogoutUseCase(
            _refreshTokenRepositoryMock.Object,
            _refreshTokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _validator);
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesToken()
    {
        // Arrange
        var request = new LogoutRequestDto { RefreshToken = "valid_token" };
        var hashedToken = "hashed_valid_token";

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns(hashedToken);

        var token = new RefreshToken(Guid.NewGuid(), hashedToken, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        await _useCase.ExecuteAsync(request, "127.0.0.1");

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedByIp.Should().Be("127.0.0.1");

        _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_WithNonExistentToken_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LogoutRequestDto { RefreshToken = "unknown_token" };

        _refreshTokenServiceMock
            .Setup(r => r.HashToken(request.RefreshToken))
            .Returns("hashed_unknown");

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync("hashed_unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
