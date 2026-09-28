using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using FlooInsurance.AuthGateway.Infrastructure.Configuration;
using FlooInsurance.AuthGateway.Infrastructure.Persistence;

namespace FlooInsurance.AuthGateway.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var adminSeedOptions = scope.ServiceProvider.GetService<IOptions<AdminSeedSettings>>()?.Value;

        // 1. Seed Roles
        var existingRoleNames = await context.Roles.Select(r => r.Name).ToListAsync();

        foreach (var roleName in RoleConstants.AllRoles)
        {
            if (!existingRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                var roleDescription = roleName switch
                {
                    RoleConstants.Customer => "Regular customer policyholder role with access to own claims and policies.",
                    RoleConstants.Admin => "System administrator with full identity and role management privileges.",
                    RoleConstants.ClaimManager => "Claims manager with assessment and claim adjustment privileges.",
                    _ => null
                };

                await context.Roles.AddAsync(new Role(Guid.NewGuid(), roleName, roleDescription));
                logger.LogInformation("Seeded role: {RoleName}", roleName);
            }
        }

        await context.SaveChangesAsync();

        // 2. Seed Initial Admin User if configured for development
        if (adminSeedOptions != null && adminSeedOptions.SeedAdminUser)
        {
            var adminEmail = adminSeedOptions.Email.Trim().ToLowerInvariant();
            var adminUser = await context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (adminUser == null)
            {
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleConstants.Admin);
                if (adminRole != null)
                {
                    var passwordHash = passwordHasher.HashPassword(adminSeedOptions.Password);
                    var newAdmin = new User(
                        adminSeedOptions.FirstName,
                        adminSeedOptions.LastName,
                        adminEmail,
                        passwordHash);

                    newAdmin.AddRole(adminRole);
                    await context.Users.AddAsync(newAdmin);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded initial administrator account for: {AdminEmail}", adminEmail);
                }
            }
        }
    }
}
