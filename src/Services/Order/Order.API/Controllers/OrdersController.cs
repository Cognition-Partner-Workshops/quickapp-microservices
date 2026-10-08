using Microsoft.AspNetCore.Mvc;
using Order.API.Mapping;
using Order.Domain.Interfaces;
using Shared.Contracts.Orders;

namespace Order.API.Controllers;

[ApiController]
[Route(OrderRoutes.Base)]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderRepository orders, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? customerId, [FromQuery] string? cashierId,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.ListAsync(customerId, cashierId, cancellationToken);
        return Ok(orders.Select(o => o.ToDto()));
    }

    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> Count([FromQuery] int? customerId, [FromQuery] string? cashierId,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.CountAsync(customerId, cashierId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order.ToDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = request.ToEntity();
        await _orders.AddAsync(order, cancellationToken);
        await _orders.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created order {OrderId} for customer {CustomerId}", order.Id, order.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order.ToDto());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        order.Discount = request.Discount;
        order.Comments = request.Comments;
        await _orders.SaveChangesAsync(cancellationToken);

        return Ok(order.ToDto());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        _orders.Remove(order);
        await _orders.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted order {OrderId}", id);

        return NoContent();
    }
}
