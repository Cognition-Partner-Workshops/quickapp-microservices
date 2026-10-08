using Order.Domain.Entities;
using Shared.Contracts.Orders;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.API.Mapping;

public static class OrderMappings
{
    public static OrderDto ToDto(this OrderEntity order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        CashierId = order.CashierId,
        Discount = order.Discount,
        Comments = order.Comments,
        Total = order.Total,
        CreatedDate = order.CreatedDate,
        UpdatedDate = order.UpdatedDate,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            Discount = i.Discount
        }).ToList()
    };

    public static OrderEntity ToEntity(this CreateOrderRequest request) => new()
    {
        CustomerId = request.CustomerId,
        CashierId = request.CashierId,
        Discount = request.Discount,
        Comments = request.Comments,
        Items = request.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            Discount = i.Discount
        }).ToList()
    };
}
