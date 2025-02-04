using ProfessionalProfiles.Entities.Models;
using System.Net;

namespace ProfessionalProfiles.Graph.General
{
    internal class UserValidationPayload(Professional? user, string message,
        HttpStatusCode code, bool isSuccess = false)
    {
        public Professional? User = user;
        public HttpStatusCode StatusCode = code;
        public bool IsSuccessful = isSuccess;
        public string Message = message;
    }
}
