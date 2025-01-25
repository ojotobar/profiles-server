using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using ProfessionalProfiles.GraphQL.General;
using System.Net;

namespace ProfessionalProfiles.Configurations
{
    public static class ExceptionMiddlewares
    {
        internal static void ConfigureExceptionHandler(this WebApplication app, ILogger<Program> logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        context.Response.StatusCode = contextFeature.Error switch
                        {
                            _ => StatusCodes.Status500InternalServerError
                        };

                        logger.LogError($"Something went wrong: {contextFeature.Error}");
                        var payload = JsonConvert.SerializeObject(GenericPayload.Initialize(contextFeature.Error?.Message ?? "An unexpected error occurred", (HttpStatusCode)context.Response.StatusCode));
                        await context.Response.WriteAsync(payload);
                    }
                });
            });
        }
    }
}
