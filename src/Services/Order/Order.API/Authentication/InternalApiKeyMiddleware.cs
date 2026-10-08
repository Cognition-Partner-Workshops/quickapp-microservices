using System.Security.Cryptography;
using System.Text;
using Shared.Contracts.Orders;

namespace Order.API.Authentication;

/// <summary>
/// Service-to-service authentication: every /api request must carry the shared internal API key.
/// Health probes stay anonymous.
/// </summary>
public sealed class InternalApiKeyMiddleware(RequestDelegate next, string apiKey)
{
    private readonly byte[] _expected = Encoding.UTF8.GetBytes(apiKey);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api") && !HasValidKey(context.Request))
        {
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: $"A valid {OrderRoutes.ApiKeyHeader} header is required.").ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    private bool HasValidKey(HttpRequest request) =>
        request.Headers.TryGetValue(OrderRoutes.ApiKeyHeader, out var provided)
        && provided.Count == 1
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided[0] ?? ""), _expected);
}
