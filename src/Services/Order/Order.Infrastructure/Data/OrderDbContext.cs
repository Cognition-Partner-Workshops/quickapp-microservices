using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    private const string PriceDecimalType = "decimal(18,2)";

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderEntity>(order =>
        {
            order.ToTable("Orders");
            order.Property(o => o.Comments).HasMaxLength(500);
            order.Property(o => o.CashierId).HasMaxLength(450);
            order.Property(o => o.Discount).HasColumnType(PriceDecimalType);
            order.HasIndex(o => o.CustomerId);
            order.HasIndex(o => o.CashierId);
            order.Ignore(o => o.Total);
            order.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("OrderItems");
            item.Property(i => i.UnitPrice).HasColumnType(PriceDecimalType);
            item.Property(i => i.Discount).HasColumnType(PriceDecimalType);
            item.HasIndex(i => i.ProductId);
            item.Ignore(i => i.LineTotal);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<OrderEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.UpdatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(o => o.CreatedDate).IsModified = false;
                entry.Entity.UpdatedDate = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
