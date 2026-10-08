namespace Shared.Contracts.Orders;

/// <summary>
/// HTTP routes exposed by order-service. Shared so callers and the service cannot drift.
/// </summary>
public static class OrderRoutes
{
    public const string Base = "api/orders";
    public const string Count = Base + "/count";

    /// <summary>Header carrying the shared key that callers must present to order-service.</summary>
    public const string ApiKeyHeader = "X-Internal-Api-Key";

    public static string ById(int id) => $"{Base}/{id}";
}
