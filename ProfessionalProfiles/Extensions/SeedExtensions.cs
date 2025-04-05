using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using Microsoft.AspNetCore.Identity;
using Mongo.Common.MongoDB;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;

namespace ProfessionalProfiles.Extensions
{
    public static class SeedExtensions
    {
        internal static async Task SeedSystemData(this WebApplication app, ILogger<Program> logger)
        {
            using var scope = app.Services.CreateScope();
            await scope.DoSeedSystemRoles(logger);
            await scope.DoSeedSystemFaqs(logger);
            await scope.DoSeedSystemUsers(logger);
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

        private static async Task DoSeedSystemUsers(this IServiceScope scope, ILogger<Program> logger)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Professional>>();
            if (userManager.IsNotNull())
            {
                var exists = userManager.Users.Any(u => u.Email != null && u.Email.Equals("blue.klikk@gmail.com"));
                if (!exists)
                {
                    var roles = Enum.GetValues(typeof(ERoles));
                    logger.LogInformation($"Starting user seeding...");
                    var user = new Professional
                    {
                        FirstName = "User",
                        LastName = "Admin",
                        Email = "blue.klikk@gmail.com",
                        PhoneNumber = "+2348035222858",
                        UserName = "blue.klikk@gmail.com",
                        Gender = EGender.Male,
                        OtherName = "",
                        Status = EStatus.Active,
                        EmailConfirmed = true,
                        IsPremium = true
                    };

                    var result = await userManager.CreateAsync(user, "P@33w0rd");
                    if (!result.Succeeded)
                    {
                        logger.LogError(result.Errors.FirstOrDefault()?.Description ?? "Could not create user");
                        return;
                    }

                    var roleResult = await userManager.AddToRoleAsync(user, ERoles.Admin.GetDescription());
                    if (!roleResult.Succeeded)
                    {
                        await userManager.DeleteAsync(user);
                        logger.LogError(roleResult.Errors.FirstOrDefault()?.Description ?? "Could not add user to role");
                        return;
                    }

                    logger.LogInformation("User registration successful");
                    return;
                }

                logger.LogInformation("Seeding skipped.... User already exist in the database.");
            }
        }
    }
}
