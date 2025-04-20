using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json;
using ProfessionalProfiles.Graph.General;
using System.Net;

namespace ProfessionalProfiles.Configurations
{
    public static class ExceptionMiddlewares
    {
        internal static void ConfigureExceptionHandler(this WebApplication app, ILogger<Program> logger)
        {
            // Put this line before UseRouting() and after UseHttpsRedirection().

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,

                // Only do this if you're behind a trusted reverse proxy
                KnownNetworks = { }, // Empty this if you want to allow all networks
                KnownProxies = { }   // Or specify your proxy's IP if needed
            });

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
