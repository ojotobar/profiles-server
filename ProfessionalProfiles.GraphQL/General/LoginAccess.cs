using ProfessionalProfiles.GraphQL.Dto;

namespace ProfessionalProfiles.GraphQL.General
{
    public class LoginAccess : BaseResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public static LoginAccess Initialize(string userName, string accessToken = "", string message = "", bool successful = false)
        {
            return new LoginAccess
            {
                Message = message,
                AccessToken = accessToken,
                UserName = userName,
                IsSuccessful = successful
            };
        }
    }
}
