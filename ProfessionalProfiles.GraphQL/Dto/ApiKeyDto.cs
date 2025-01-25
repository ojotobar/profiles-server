using System.Net;

namespace ProfessionalProfiles.GraphQL.Dto
{
    public class ApiKeyDto : BaseResponseDto
    {
        public string ApiKey { get; set; } = string.Empty;

        public static ApiKeyDto Initialize(string apiKey, string message, HttpStatusCode statusCode, bool isSuccess = false)
        {
            return new ApiKeyDto
            {
                ApiKey = apiKey,
                Message = message,
                StatusCode = statusCode,
                IsSuccessful = isSuccess
            };
        }
    }
}
