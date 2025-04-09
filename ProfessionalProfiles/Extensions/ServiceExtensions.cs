using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProfessionalProfiles.Data.Implementations;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Implementations;
using ProfessionalProfiles.Services.Interfaces;
using System.Text;

namespace ProfessionalProfiles.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureDataAndServices(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();
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
                    builder.WithOrigins("http://localhost:4200", "http://localhost:4300")
                        .AllowCredentials()
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
            });
        }
    }
}
