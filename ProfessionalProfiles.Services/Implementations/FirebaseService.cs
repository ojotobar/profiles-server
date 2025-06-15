using CSharpTypes.Extensions.Enumeration;
using Firebase.Auth;
using Firebase.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Services.Interfaces;
using Newtonsoft.Json;
using System;
using Mailjet.Client.Resources;
using System.Net.Http.Headers;
using AspNetCore.Identity.MongoDbCore.Models;
using System.Net.Sockets;

namespace ProfessionalProfiles.Services.Implementations
{
    public class FirebaseService(IConfiguration configuration, ILogger<FirebaseService> logger) : IFirebaseService
    {
        private readonly string ApiKey = configuration["Firebase:ApiKey"]!;
        private readonly string Email = configuration["Firebase:Email"]!;
        private readonly string Password = configuration["Firebase:Password"]!;
        private readonly string Bucket = configuration["Firebase:Bucket"]!;
        private readonly string IntBucket = configuration["Firebase:IntBucket"]!;
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

        public async Task RemoveFileAsync(ECloudFolder folder, string fileName)
        {
            try
            {
                var user = await GetCredential();

                await new FirebaseStorage(Bucket, new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(user.FirebaseToken)
                }).Child(folder.GetDescription()).Child(fileName).DeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(JsonConvert.SerializeObject(ex));
            }
        }

        public async Task CleanFolderAsync(bool deleteAll = false)
        {
            const string firebaseStorageBaseUrl = "https://firebasestorage.googleapis.com/storage/v1/b";

            var fireBaseAuthLink = await GetCredential();
            if(fireBaseAuthLink != null && !string.IsNullOrWhiteSpace(fireBaseAuthLink.FirebaseToken))
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fireBaseAuthLink.FirebaseToken);

                var folders = Enum.GetValues(typeof(ECloudFolder))
                .Cast<ECloudFolder>()
                .ToList();

                for (int i = 0; i < folders.Count; i++)
                {
                    var folderUrl = $"{firebaseStorageBaseUrl}/{IntBucket}/o?prefix={folders[i].GetDescription()}/";

                    var listResponse = await client.GetAsync(folderUrl);
                    var listContent = await listResponse.Content.ReadAsStringAsync();

                    if (!listResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"{nameof(CleanFolderAsync)}: ❌ Failed to list files: " + listContent);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(listContent))
                    {
                        dynamic? json = JsonConvert.DeserializeObject(listContent);
                        if (json != null)
                        {
                            var items = json.items;

                            if (items == null)
                            {
                                _logger.LogError($"{nameof(CleanFolderAsync)}: 📂 No files found under folder: {folders[i]}");
                                continue;
                            }

                            foreach (var item in items)
                            {
                                string name = item.name;
                                var encodedName = Uri.EscapeDataString(name);
                                if (!encodedName.Contains(folders[i].ToString()) || deleteAll)
                                {
                                    var deleteUrl = $"{firebaseStorageBaseUrl}/{IntBucket}/o/{encodedName}";
                                    _logger.LogInformation($"{nameof(CleanFolderAsync)}: Deleting object at path: {deleteUrl}");


                                    //var deleteResponse = await client.DeleteAsync(deleteUrl);
                                    //var deleteResult = await deleteResponse.Content.ReadAsStringAsync();

                                    //if (deleteResponse.IsSuccessStatusCode)
                                    //{
                                    //    _logger.LogInformation($"{nameof(CleanFolderAsync)}: ✅ Deleted: {name}");
                                    //}
                                    //else
                                    //{
                                    //    _logger.LogInformation($"{nameof(CleanFolderAsync)}: ❌ Failed to delete {name}: {deleteResult}");
                                    //}
                                }
                            }

                            _logger.LogInformation($"{nameof(CleanFolderAsync)}: \U0001f9f9 Cleanup complete.");
                        }
                        else
                        {
                            _logger.LogError($"{nameof(CleanFolderAsync)}: Deserialized list content returned null");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogError($"{nameof(CleanFolderAsync)}: Invalid content received as list: {listContent}");
                    }
                }
            }
            else
            {
                _logger.LogError($"{nameof(CleanFolderAsync)}: Invalid credential!!! Firebase Auth Link is null or the Token is null or empty!!!");
                return;
            }
        }

        private async Task<FirebaseAuthLink> GetCredential()
        {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
            return await authProvider.SignInWithEmailAndPasswordAsync(Email, Password);
        }
    }
}
