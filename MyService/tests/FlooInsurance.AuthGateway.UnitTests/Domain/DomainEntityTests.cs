using FluentAssertions;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using FlooInsurance.AuthGateway.Domain.Exceptions;
using FlooInsurance.AuthGateway.Domain.ValueObjects;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Domain;

public class DomainEntityTests
{
    [Fact]
    public void Email_Create_WithValidEmail_ReturnsNormalizedEmail()
    {
        // Arrange
        var input = "  John.Doe@Example.COM  ";

        // Act
        var email = Email.Create(input);

        // Assert
        email.Value.Should().Be("john.doe@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public void Email_Create_WithInvalidEmail_ThrowsDomainException(string? invalidEmail)
    {
        // Act
        var act = () => Email.Create(invalidEmail);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Email_Equals_WithCaseInsensitiveMatch_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("USER@EXAMPLE.COM");

        // Act & Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeFalse(); // reference check vs equality
        email1.Equals((object)email2).Should().BeTrue();
    }

    [Fact]
    public void User_Create_WithValidData_InitializesProperties()
    {
        // Arrange & Act
        var user = new User("John", "Doe", "john@example.com", "hashed_password");

        // Assert
        user.Id.Should().NotBeEmpty();
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john@example.com");
        user.PasswordHash.Should().Be("hashed_password");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void User_DeactivateAndActivate_UpdatesStateAndTimestamp()
    {
        // Arrange
        var user = new User("John", "Doe", "john@example.com", "hashed_password");

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().NotBeNull();

        // Act again
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var token = new RefreshToken(Guid.NewGuid(), "hash123", now, now.AddDays(7));

        // Act & Assert
        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Revoke_MarksTokenRevokedAndSetsProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var token = new RefreshToken(Guid.NewGuid(), "hash123", now, now.AddDays(7));

        // Act
        token.Revoke("127.0.0.1", "replacementHash456");

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedByIp.Should().Be("127.0.0.1");
        token.ReplacedByTokenHash.Should().Be("replacementHash456");
        token.RevokedAt.Should().NotBeNull();
    }
}
