using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Services.Interfaces
{
    public interface IFirebaseService
    {
        Task<(string Link, bool Success)> UploadFileAsync(Stream stream, ECloudFolder folder, string fileName, CancellationToken cancellation);
    }
}
