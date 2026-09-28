namespace FlooInsurance.AuthGateway.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "FlooInsuranceCorsPolicy";

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
