using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Domain.Interfaces;

public interface IOrderRepository
{
    Task<IReadOnlyList<OrderEntity>> ListAsync(int? customerId, string? cashierId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(int? customerId, string? cashierId, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default);
    void Remove(OrderEntity order);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
