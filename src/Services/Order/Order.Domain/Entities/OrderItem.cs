namespace Order.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    /// <summary>Product id owned by the product bounded context (monolith today).</summary>
    public int ProductId { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }

    public decimal LineTotal => UnitPrice * Quantity - Discount;
}
