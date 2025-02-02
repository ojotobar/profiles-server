using System.Net;

namespace ProfessionalProfiles.Graph.Dto
{
    public abstract class BaseResponseDto
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
