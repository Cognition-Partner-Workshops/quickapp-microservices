using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Orders;

public sealed record CreateOrderRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    [StringLength(450)]
    public string? CashierId { get; init; }

    [Range(0, double.MaxValue), MaxDecimalPlaces(2)]
    public decimal Discount { get; init; }

    [StringLength(500)]
    public string? Comments { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyList<CreateOrderItemRequest> Items { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        decimal subtotal;
        try
        {
            subtotal = Items.Sum(i => i.UnitPrice * i.Quantity - i.Discount);
        }
        catch (OverflowException)
        {
            return [new ValidationResult("The order total is too large.", [nameof(Items)])];
        }

        return Discount > subtotal
            ? [new ValidationResult("The order discount must not exceed the sum of the line totals.", [nameof(Discount)])]
            : [];
    }
}

public sealed record CreateOrderItemRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(0, double.MaxValue), MaxDecimalPlaces(2)]
    public decimal UnitPrice { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [Range(0, double.MaxValue), MaxDecimalPlaces(2)]
    public decimal Discount { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        decimal extended;
        try
        {
            extended = UnitPrice * Quantity;
        }
        catch (OverflowException)
        {
            return [new ValidationResult("Unit price times quantity is too large.", [nameof(UnitPrice)])];
        }

        return Discount > extended
            ? [new ValidationResult("The item discount must not exceed unit price times quantity.", [nameof(Discount)])]
            : [];
    }
}
