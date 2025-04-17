using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProfessionalProfiles.Data.Implementations;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Implementations;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Services.Jobs;
using Quartz;
using System.Security.Claims;
using System.Text;

namespace ProfessionalProfiles.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureQuartz(this IServiceCollection services)
        {
            services.AddQuartz()
                .AddScoped<AuditLogJob>()
                .AddScoped<SendRoleUpdateNotification>()
                .AddScoped<StatusChangeNotification>()
                .AddScoped<BackgroundJobsWorker>();
        }
        public static void ConfigureDataAndServices(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();
            services.AddScoped<BackgroundJobsWorker>();
            services.AddScoped<IServiceManager, ServiceManager>();
        }

        public static void ConfigureMongoIdentity(this IServiceCollection services, 
            string connectionString, string databaseName)
        {
            services.AddIdentity<Professional, AppRole>()
                .AddMongoDbStores<Professional, AppRole, Guid>(connectionString, databaseName)
                .AddDefaultTokenProviders();
        }

        public static void ConfigureCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

        public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Authorization:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authorization:SecretKey"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if(context != null)
                        {
                            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Professional>>();
                            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                            if (string.IsNullOrEmpty(userId))
                            {
                                context.Fail("Unauthorized: User not found");
                                return;
                            }

                            var user = await userManager.FindByIdAsync(userId);
                            if (user != null)
                            {
                                if(user.Status == EStatus.Inactive && user.DeactivatedOn < DateTime.MaxValue)
                                {
                                    context.Fail("Unauthorized: User is deactivated");
                                }
                                else if(user.Status == EStatus.Suspended && user.IsDeprecated)
                                {
                                    context.Fail("Unauthorized: User is suspended");
                                }
                            }
                            else
                            {
                                context.Fail("Unauthorized: User not found");
                            }
                        }
                    }
                };
            });
        }
    }
}
