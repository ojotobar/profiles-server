using HotChocolate.AspNetCore;
using HotChocolate.Execution;

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
            
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
