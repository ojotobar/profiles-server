using CSharpTypes.Extensions.Guid;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Shared.DTOs;
using ProfessionalProfiles.Shared.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProfessionalProfiles.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<Professional> userManager;
        private readonly SignInManager<Professional> signInManager;
        private readonly IConfiguration configuration;

        public UserService(UserManager<Professional> userManager,
            SignInManager<Professional> signInManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        public async Task<(bool Valid, string Message)> IsValidApiKey(string base64String)
        {
            if (base64String.IsNullOrEmpty())
            {
                return (false, "Invalid Api Key");
            }

            var userId = base64String.DecodeBase64StringAsGuid();
            if (userId.IsEmpty())
            {
                return (false, "Invalid Api Key");
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if(user == null)
            {
                return (false, "User not found");
            }

            var isValid = base64String.IsValidApiKey(userId, user.KeyMarker);
            if (!isValid)
            {
                return (isValid, "Invalid API Key");
            }
            return (isValid, "API Key Validated");
        }

        public async Task<AccessTokenDto> Validate(string email, string password)
        {
            var response = new AccessTokenDto();
            var user = await userManager.FindByNameAsync(email);
            if (user == null)
            {
                response.Message = "No user found with this email: {request.Email}";
                return response;
            }

            var signinResult = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, true);
            if (signinResult.Succeeded && user.EmailConfirmed && user.DeactivatedOn <= DateTime.MaxValue)
            {
                if (user.Status == EStatus.Inactive && !user.IsDeprecated) //For users that deactivated, reactivate and log them in
                {
                    user.Status = EStatus.Active;
                    user.IsDeprecated = false;
                    user.UpdatedOn = DateTime.Now;
                    user.DeactivatedOn = DateTime.MaxValue;
                }

                user.LastLogin = DateTime.Now;
                await userManager.UpdateAsync(user);
                response.Message = "Login successful";
                response.UserName = user.UserName;
                response.Successful = signinResult.Succeeded;

                return response;
            }
            else
            {
                return HandleLoginError(user, response);
            }
        }

        public async Task<AccessTokenDto> CreateAccessToken(AccessTokenDto tokenDto, Professional user)
        {
            var signingCredentials = GetSigningCredentials();

            var claims = await GetClaims(user);

            var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
            
            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            tokenDto.AccessToken = accessToken;
            return tokenDto;
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(configuration["Authorization:SecretKey"]!);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims(Professional user)
        {
            var claims = new List<Claim> 
            { 
                    new Claim(ClaimTypes.Name, user?.UserName!), 
                    new Claim(ClaimTypes.NameIdentifier.ToString(), user?.Id.ToString()!) 
            };

            var roles = await userManager.GetRolesAsync(user!);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken
            (
                issuer: configuration["Authorization:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(configuration["Authorization:Expires"])),
                signingCredentials: signingCredentials
            );
            return tokenOptions;
        }

        private AccessTokenDto HandleLoginError(Professional user, AccessTokenDto response)
        {
            if (!user.EmailConfirmed)
            {
                response.EmailNotConfirmed = true;
                response.Message = "Email not confirmed. Please confirm your account before attempting to login. Confirmation code sent to your email.";
            }

            else if (user.Status == EStatus.Inactive && user.IsDeprecated)
            {
                response.Message = "Access denied. Account not deactivated. Please submit a support ticket to reactivate your account.";
            }

            else
            {
                response.Message = "Wrong email or password.";
            }

            return response;
        }
    }
}
