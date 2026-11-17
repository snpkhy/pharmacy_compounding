using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Services
{
    // Time events of the state diagram (lab 1), checked at startup and then every hour:
    //   Awaiting payment → after(3 days) → Cancelled
    //   Ready → after(10 days) / dispose → Cancelled (Cancelled: entry / refund if paid)
    public class OrderExpirationService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<OrderExpirationService> _log;

        public OrderExpirationService(IServiceScopeFactory scopes, ILogger<OrderExpirationService> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    var cancelled = await CancelExpiredAsync(stoppingToken);
                    if (cancelled > 0) _log.LogInformation("Automatically cancelled {Count} expired orders", cancelled);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Someone changed one of these orders at the same moment: it is checked again on the next run
                    _log.LogInformation("Expired orders were changed by a user, will retry on the next run");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "Failed to cancel expired orders");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        public async Task<int> CancelExpiredAsync(CancellationToken ct = default)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PharmacyContext>();
            var now = DateTime.Now;
            var unpaidBefore = now - Order.PaymentTimeout;
            var notPickedUpBefore = now - Order.PickupTimeout;

            var orders = await db.Orders
                .Include(o => o.Payment)
                .Where(o => o.Status == OrderStatus.AwaitingPayment && o.StatusChangedAt <= unpaidBefore ||
                            o.Status == OrderStatus.Ready && o.StatusChangedAt <= notPickedUpBefore)
                .ToListAsync(ct);

            foreach (var order in orders)
                order.Cancel(order.Status == OrderStatus.AwaitingPayment
                    ? $"Скасовано автоматично: замовлення не оплачено протягом {Order.PaymentTimeout.Days} днів"
                    : $"Скасовано автоматично: ліки не забрали протягом {Order.PickupTimeout.Days} днів, їх утилізовано");

            // All cancellations (statuses and refunds) are saved in one transaction
            await db.SaveChangesAsync(ct);
            return orders.Count;
        }
    }
}
