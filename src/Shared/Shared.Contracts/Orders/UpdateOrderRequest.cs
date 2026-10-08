using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Orders;

public sealed record UpdateOrderRequest
{
    [Range(0, double.MaxValue)]
    public decimal Discount { get; init; }

    [StringLength(500)]
    public string? Comments { get; init; }
}
