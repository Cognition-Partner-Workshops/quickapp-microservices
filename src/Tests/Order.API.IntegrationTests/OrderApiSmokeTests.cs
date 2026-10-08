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

    [Theory]
    [InlineData(1.999, 1, 0, 0)]   // fractional cents would be rounded by decimal(18,2)
    [InlineData(10, 1, 0.005, 0)]
    [InlineData(10, 1, 20, 0)]     // item discount > unit price * quantity
    [InlineData(10, 1, 0, 20)]     // order discount > line totals
    public async Task Create_rejects_invalid_amounts(double unitPrice, int quantity, double itemDiscount,
        double orderDiscount)
    {
        var response = await _client.PostAsJsonAsync(OrderRoutes.Base, new CreateOrderRequest
        {
            CustomerId = 1,
            Discount = (decimal)orderDiscount,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = 1, UnitPrice = (decimal)unitPrice, Quantity = quantity, Discount = (decimal)itemDiscount
                }
            ]
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_rejects_invalid_discounts_and_totals_match_after_reload()
    {
        var createResponse = await _client.PostAsJsonAsync(OrderRoutes.Base, new CreateOrderRequest
        {
            CustomerId = 7,
            Items = [new CreateOrderItemRequest { ProductId = 1, UnitPrice = 10.25m, Quantity = 1 }]
        });
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        var fetched = await _client.GetFromJsonAsync<OrderDto>(OrderRoutes.ById(created!.Id));
        Assert.Equal(created.Total, fetched!.Total);

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(OrderRoutes.ById(created.Id),
            new UpdateOrderRequest { Discount = 20m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(OrderRoutes.ById(created.Id),
            new UpdateOrderRequest { Discount = 1.005m })).StatusCode);

        var updated = await (await _client.PutAsJsonAsync(OrderRoutes.ById(created.Id),
            new UpdateOrderRequest { Discount = 10.25m })).Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(0m, updated!.Total);
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
