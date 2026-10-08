using System.Net;
using System.Net.Http.Json;
using Shared.Contracts.Orders;

namespace Order.API.IntegrationTests;

public class OrderApiSmokeTests : IClassFixture<OrderServiceFactory>
{
    private readonly HttpClient _client;

    public OrderApiSmokeTests(OrderServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_endpoints_report_healthy()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/healthz")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/readyz")).StatusCode);
    }

    [Fact]
    public async Task Order_lifecycle_round_trips_through_postgres()
    {
        var cashierId = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest
        {
            CustomerId = 42,
            CashierId = cashierId,
            Discount = 10m,
            Comments = "smoke test",
            Items =
            [
                new CreateOrderItemRequest { ProductId = 1, UnitPrice = 100m, Quantity = 2, Discount = 5m },
                new CreateOrderItemRequest { ProductId = 2, UnitPrice = 50m, Quantity = 1 }
            ]
        };

        var createResponse = await _client.PostAsJsonAsync(OrderRoutes.Base, request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        Assert.NotNull(createResponse.Headers.Location);
        Assert.Equal(2, created.Items.Count);
        Assert.Equal(235m, created.Total); // (100*2 - 5) + 50 - 10

        var fetched = await _client.GetFromJsonAsync<OrderDto>(OrderRoutes.ById(created.Id));
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(cashierId, fetched.CashierId);

        var byCustomer = await _client.GetFromJsonAsync<List<OrderDto>>($"{OrderRoutes.Base}?customerId=42");
        Assert.Contains(byCustomer!, o => o.Id == created.Id);

        var count = await _client.GetFromJsonAsync<int>($"{OrderRoutes.Count}?cashierId={cashierId}");
        Assert.Equal(1, count);

        var updateResponse = await _client.PutAsJsonAsync(OrderRoutes.ById(created.Id),
            new UpdateOrderRequest { Discount = 0m, Comments = "updated" });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("updated", updated!.Comments);
        Assert.Equal(245m, updated.Total);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync(OrderRoutes.ById(created.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(OrderRoutes.ById(created.Id))).StatusCode);
    }

    [Fact]
    public async Task Create_rejects_order_without_items()
    {
        var response = await _client.PostAsJsonAsync(OrderRoutes.Base, new CreateOrderRequest { CustomerId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Correlation_id_is_echoed_back()
    {
        var correlationId = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, OrderRoutes.Base);
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
    }
}
