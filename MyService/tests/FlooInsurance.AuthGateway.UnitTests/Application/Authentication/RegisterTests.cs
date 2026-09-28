using FluentAssertions;
using FluentValidation;
using Moq;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Register;
using FlooInsurance.AuthGateway.Application.Validators;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.UnitTests.Application.Authentication;

public class RegisterTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IValidator<RegisterRequestDto> _validator;
    private readonly RegisterUserUseCase _useCase;

    public RegisterTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validator = new RegisterRequestValidator();

        _useCase = new RegisterUserUseCase(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _validator);
    }

    [Fact]
    public async Task Register_WithValidData_CreatesCustomerUser()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync("john.doe@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var customerRole = new Role(Guid.NewGuid(), RoleConstants.Customer, "Customer role");
        _roleRepositoryMock
            .Setup(r => r.GetByNameAsync(RoleConstants.Customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerRole);

        _passwordHasherMock
            .Setup(p => p.HashPassword(request.Password))
            .Returns("hashed_secure_password");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("john.doe@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Roles.Should().ContainSingle(r => r == RoleConstants.Customer);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "john.doe@example.com" &&
            u.PasswordHash == "hashed_secure_password" &&
            u.UserRoles.Any(ur => ur.Role.Name == RoleConstants.Customer)
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoSpecialChar123")]
    [InlineData("NoDigitSpecial!")]
    public async Task Register_WithWeakPassword_ThrowsValidationException(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        };

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_WithPasswordMismatch_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "Password123!",
            ConfirmPassword = "MismatchPassword123!"
        };

        // Act
        var act = async () => await _useCase.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
