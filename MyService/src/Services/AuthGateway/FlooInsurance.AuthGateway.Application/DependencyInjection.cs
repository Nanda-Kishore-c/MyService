using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FlooInsurance.AuthGateway.Application.Features.Authentication.CurrentUser;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Login;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Logout;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Refresh;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Register;
using FlooInsurance.AuthGateway.Application.Features.Authentication.Revoke;
using FlooInsurance.AuthGateway.Application.Features.Roles.AssignRole;
using FlooInsurance.AuthGateway.Application.Features.Roles.GetRoles;
using FlooInsurance.AuthGateway.Application.Features.Roles.RemoveRole;

namespace FlooInsurance.AuthGateway.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Authentication Use Cases
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();
        services.AddScoped<IRevokeTokenUseCase, RevokeTokenUseCase>();
        services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();

        // Role Management Use Cases
        services.AddScoped<IAssignRoleUseCase, AssignRoleUseCase>();
        services.AddScoped<IRemoveRoleUseCase, RemoveRoleUseCase>();
        services.AddScoped<IGetUserRolesUseCase, GetUserRolesUseCase>();

        return services;
    }
}
