using ProfessionalProfiles.Graph.Dto;
using System.Net;

namespace ProfessionalProfiles.Graph.General
{
    public class GenericPayload : BaseResponseDto
    {
        public static GenericPayload Initialize(string message, HttpStatusCode code, bool isSuccess = false)
        {
            return new GenericPayload { Message = message, StatusCode = code, IsSuccessful = isSuccess };
        }
    }
}
