using Serilog;
using FlooInsurance.AuthGateway.API.Extensions;
using FlooInsurance.AuthGateway.API.Middleware;
using FlooInsurance.AuthGateway.Application;
using FlooInsurance.AuthGateway.Infrastructure;
using FlooInsurance.AuthGateway.Infrastructure.Seeding;

// Initialize early bootstrap Serilog logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting FlooInsurance AuthGateway API...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // Register Clean Architecture layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices();

    // Register Cross-cutting API services
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddCustomCors(builder.Configuration);
    builder.Services.AddControllers();

    var app = builder.Build();

    // Pipeline: Exception handling -> Correlation ID -> Structured Logging -> HTTPS -> CORS -> AuthN -> AuthZ -> Endpoints
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlooInsurance AuthGateway v1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root "/"
        });
    }

    app.UseHttpsRedirection();
    app.UseCors(CorsExtensions.PolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Controlled automatic role seeding and development admin setup
    try
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Initial database seeding skipped or deferred: {Message}", ex.Message);
    }

    Log.Information("FlooInsurance AuthGateway started successfully.");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FlooInsurance AuthGateway terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
