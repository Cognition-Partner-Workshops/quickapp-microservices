namespace Shared.Contracts.Orders;

public sealed record OrderDto
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string? CashierId { get; init; }
    public decimal Discount { get; init; }
    public string? Comments { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime UpdatedDate { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
}

public sealed record OrderItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Discount { get; init; }
}
