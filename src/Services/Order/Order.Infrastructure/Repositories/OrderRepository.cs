using Microsoft.EntityFrameworkCore;
using Order.Domain.Interfaces;
using Order.Infrastructure.Data;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrderEntity>> ListAsync(int? customerId, string? cashierId,
        CancellationToken cancellationToken = default)
    {
        return await Filter(customerId, cashierId)
            .Include(o => o.Items)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(int? customerId, string? cashierId, CancellationToken cancellationToken = default)
    {
        return Filter(customerId, cashierId).CountAsync(cancellationToken);
    }

    public Task<OrderEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public void Remove(OrderEntity order)
    {
        _context.Orders.Remove(order);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<OrderEntity> Filter(int? customerId, string? cashierId)
    {
        var query = _context.Orders.AsQueryable();

        if (customerId is not null)
            query = query.Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrEmpty(cashierId))
            query = query.Where(o => o.CashierId == cashierId);

        return query;
    }
}
