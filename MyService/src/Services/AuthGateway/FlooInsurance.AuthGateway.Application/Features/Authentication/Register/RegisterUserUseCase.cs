using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Register;

public class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterRequestDto> _validator;

    public RegisterUserUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IValidator<RegisterRequestDto> validator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<RegisterResponseDto> ExecuteAsync(
        RegisterRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var customerRole = await _roleRepository.GetByNameAsync(RoleConstants.Customer, cancellationToken);
        if (customerRole == null)
        {
            throw new NotFoundException("Role", RoleConstants.Customer);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(
            request.FirstName,
            request.LastName,
            normalizedEmail,
            passwordHash);

        // Security requirement: public registration ALWAYS assigns Customer role only
        user.AddRole(customerRole);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = new[] { RoleConstants.Customer },
            Message = "User registered successfully.",
            CreatedAt = user.CreatedAt
        };
    }
}
