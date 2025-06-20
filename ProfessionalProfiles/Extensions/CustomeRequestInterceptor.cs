using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using ProfessionalProfiles.Entities.Models;
using UAParser;

namespace ProfessionalProfiles.Extensions
{
    public class CustomeRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder, CancellationToken cancellationToken)
        {
            var key = string.Empty;
            if(context.Request.Headers.TryGetValue("X-PPAPI-KEY", out var keyValue))
            {
                key = keyValue.ToString();
            };

            var appTag = string.Empty;
            if(context.Request.Headers.TryGetValue("X-CLIENT-TAG", out var appTagValue))
            {
                appTag = appTagValue.ToString();
            };

            var isPremium = false;
            if (context.Request.Headers.TryGetValue("X-IS-PREMIUM", out var headerValue))
            {
                _ = bool.TryParse(headerValue.ToString(), out isPremium);
            }

            context.Request.Headers.TryGetValue("Origin", out var origin);
            requestBuilder.SetGlobalState("origin", (string?)origin);

            requestBuilder.SetGlobalState("auditLog", new AuditLog
            {
                Platform = ParseUA(context.Request),
                IPAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() 
                    ?? context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            });

            requestBuilder.SetGlobalState("apiAccessInput", new ApiAccessInput(key, appTag, isPremium));

            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }

        private static string ParseUA(HttpRequest httpRequest)
        {
            var userAgent = httpRequest.Headers.UserAgent.ToString();
            var uaParser = Parser.GetDefault();
            ClientInfo client = uaParser.Parse(userAgent);

            string browser = $"{client.UA.Family} {client.UA.Major}";
            string os = $"{client.OS.Family} {client.OS.Major}";

            return $"{browser} on {os}";
        }
    }
}
