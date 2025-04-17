using Amazon.Runtime.Internal;
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
            context.Request.Headers.TryGetValue("X-PPAPI-KEY", out var key);
            requestBuilder.SetGlobalState("apiKey", (string?)key);

            context.Request.Headers.TryGetValue("Origin", out var origin);
            requestBuilder.SetGlobalState("origin", (string?)origin);

            requestBuilder.SetGlobalState("auditLog", new AuditLog
            {
                Platform = ParseUA(context.Request),
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
            });

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
