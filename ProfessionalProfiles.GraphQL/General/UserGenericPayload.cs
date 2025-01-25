using ProfessionalProfiles.GraphQL.Dto;
using System.Net;

namespace ProfessionalProfiles.GraphQL.General
{
    public class UserGenericPayload : BaseResponseDto
    {
        public string? Email { get; set; } = string.Empty;

        public static UserGenericPayload Initialize(string? email, string message, HttpStatusCode code, bool isSuccess = false)
        {
            return new UserGenericPayload { Email = email, Message = message, StatusCode = code, IsSuccessful = isSuccess };
        }
    }
}
