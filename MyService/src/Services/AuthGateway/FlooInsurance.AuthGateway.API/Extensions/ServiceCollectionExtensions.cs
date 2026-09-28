using FlooInsurance.AuthGateway.API.Services;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;

namespace FlooInsurance.AuthGateway.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
