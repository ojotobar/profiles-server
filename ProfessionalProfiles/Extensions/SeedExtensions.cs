using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Extensions
{
    public static class SeedExtensions
    {
        internal static async Task SeedSystemData(this WebApplication app, ILogger<Program> logger)
        {
            using var scope = app.Services.CreateScope();
            await scope.DoSeedSystemRoles(logger);
        }

        private static async Task DoSeedSystemRoles(this IServiceScope scope, ILogger<Program> logger)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            if (roleManager.IsNotNull())
            {
                var existingRoles = roleManager.Roles.ToList();
                if (!existingRoles.IsNotNullOrEmpty())
                {
                    var roles = Enum.GetValues(typeof(ERoles));
                    logger.LogInformation($"Starting roles seeding: {string.Join(",", roles)}");
                    foreach (var role in roles)
                    {
                        var roleName = role.GetDescription();
                        if (roleName.IsNullOrEmpty())
                        {
                            logger.LogError("Role Name is required");
                            continue;
                        }

                        var result = await roleManager.CreateAsync(new AppRole
                        {
                            Name = roleName,
                            NormalizedName = roleName.ToUpper()
                        });

                        if (!result.Succeeded)
                        {
                            var errorMessages = string.Join(Environment.NewLine, result.Errors);
                            logger.LogError($"Error occurred: {errorMessages}");
                            continue;
                        }
                        logger.LogInformation($"Role, {role} successfully added");
                    }
                    return;
                }

                logger.LogInformation("Seeding skipped.... Roles already exist in the database.");
            }
        }
    }
}
