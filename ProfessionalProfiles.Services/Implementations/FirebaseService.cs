using CSharpTypes.Extensions.Enumeration;
using Firebase.Auth;
using Firebase.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Services.Interfaces;
using Newtonsoft.Json;

namespace ProfessionalProfiles.Services.Implementations
{
    public class FirebaseService(IConfiguration configuration, ILogger<FirebaseService> logger) : IFirebaseService
    {
        private readonly string ApiKey = configuration["Firebase:ApiKey"]!;
        private readonly string Email = configuration["Firebase:Email"]!;
        private readonly string Password = configuration["Firebase:Password"]!;
        private readonly string Bucket = configuration["Firebase:Bucket"]!;
        private readonly ILogger<FirebaseService> _logger = logger;

        public async Task<(string Link, bool Success)> UploadFileAsync(Stream stream, ECloudFolder folder, 
            string fileName, CancellationToken cancellation)
        {
            try
            {
                if (stream.Length <= 0)
                {
                    _logger.LogError("Invalid file.");
                    return ("", false);
                }

                var user = await GetCredential();
                var store = new FirebaseStorage(Bucket, new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(user.FirebaseToken)
                }).Child(folder.GetDescription()).Child(fileName).PutAsync(stream, cancellation);

                var link = await store;
                return (link, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(JsonConvert.SerializeObject(ex));
                return ("", false);
            }
        }

        private async Task<FirebaseAuthLink> GetCredential()
        {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            return await authProvider.SignInWithEmailAndPasswordAsync(Email, Password);
        }
    }
}
