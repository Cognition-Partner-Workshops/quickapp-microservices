using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Orders;

/// <summary>
/// Rejects decimals with more fractional digits than the store keeps, so responses never differ from persisted values.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxDecimalPlacesAttribute(int places) : ValidationAttribute(
    $"The field {{0}} must not have more than {places} decimal places.")
{
    public int Places { get; } = places;

    public override bool IsValid(object? value) =>
        value is not decimal d || decimal.Round(d, Places) == d;
}
