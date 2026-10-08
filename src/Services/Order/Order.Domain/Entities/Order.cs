namespace Order.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    /// <summary>Customer id owned by the customer bounded context (monolith today).</summary>
    public int CustomerId { get; set; }

    /// <summary>Identity user id of the cashier who processed the order.</summary>
    public string? CashierId { get; set; }

    public decimal Discount { get; set; }
    public string? Comments { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Total => Items.Sum(i => i.LineTotal) - Discount;
}
