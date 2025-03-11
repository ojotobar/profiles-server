using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
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
            await scope.DoSeedSystemFaqs(logger);
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

        private static async Task DoSeedSystemFaqs(this IServiceScope scope, ILogger<Program> logger)
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();
            if (repository.IsNotNull())
            {
                var faqsExists = await repository.Faqs.HasAnyAsync(f => !f.IsDeprecated);
                if (!faqsExists)
                {
                    var faqs = SeederExtensions.GetSystemFaqs();
                    logger.LogInformation($"Starting FAQs seeding...");

                    await repository.Faqs.AddRangeAsync(faqs);
                    logger.LogInformation($"{faqs.Count} FAQs successfully added to the database.");
                    return;
                }

                logger.LogInformation("FAQs Seeding skipped.... FAQs already exist in the database.");
            }
        }
    }
}
