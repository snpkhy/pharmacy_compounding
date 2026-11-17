using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Order: the central class of the domain
    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Номер")]
        public int Number { get; set; }

        [Display(Name = "Дата створення")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Планова дата готовності")]
        public DateTime PlannedReadyDate { get; set; } = DateTime.Today.AddDays(3);

        [Display(Name = "Статус")]
        public OrderStatus Status { get; set; } = OrderStatus.Created;

        // When the order entered its current status: the issue date for "Issued" (state diagram: entry / record
        // the issue date) and the start of the time events after(3 days) and after(10 days)
        [Display(Name = "Дата зміни статусу")]
        public DateTime StatusChangedAt { get; set; } = DateTime.Now;

        // Why the order was rejected or cancelled: shown to the client (state diagram, "Rejected": entry / notify the client)
        [StringLength(500)]
        [Display(Name = "Причина")]
        public string? StatusReason { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Сума, грн")]
        public decimal Total { get; set; }

        [Display(Name = "Клієнт")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Display(Name = "Рецепт")]
        public int PrescriptionId { get; set; }
        public Prescription? Prescription { get; set; }

        public List<OrderItem> Items { get; set; } = new();
        public Payment? Payment { get; set; }
        public List<ManufacturingAct> ManufacturingActs { get; set; } = new();

        // Row version (PostgreSQL xmin): detects that someone else changed the order since it was read
        public uint Version { get; set; }

        public void AddItem(OrderItem item) => Items.Add(item);

        public decimal CalculateTotal()
        {
            Total = Items.Sum(i => i.CalculateCost());
            return Total;
        }

        // Allowed transitions, matching the state diagram from lab 1
        private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
        {
            [OrderStatus.Created] = new[] { OrderStatus.UnderReview, OrderStatus.Cancelled },
            [OrderStatus.UnderReview] = new[] { OrderStatus.AwaitingPayment, OrderStatus.Rejected },
            [OrderStatus.AwaitingPayment] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new[] { OrderStatus.InProduction, OrderStatus.AwaitingIngredients, OrderStatus.Cancelled },
            [OrderStatus.AwaitingIngredients] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
            [OrderStatus.InProduction] = new[] { OrderStatus.QualityControl },
            [OrderStatus.QualityControl] = new[] { OrderStatus.Ready, OrderStatus.InProduction },
            [OrderStatus.Ready] = new[] { OrderStatus.Issued, OrderStatus.Cancelled },
            [OrderStatus.Rejected] = Array.Empty<OrderStatus>(),
            [OrderStatus.Issued] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        public bool CanChangeTo(OrderStatus next) => Transitions[Status].Contains(next);

        // The order and its prescription can be corrected until the pharmacist has verified the prescription
        public bool IsEditable => Status is OrderStatus.Created or OrderStatus.UnderReview;

        // Only orders that never reached manufacturing and hold no money can be deleted
        public bool CanBeDeleted =>
            (Status is OrderStatus.Created or OrderStatus.UnderReview or OrderStatus.Rejected or OrderStatus.Cancelled) &&
            !ManufacturingActs.Any() && Payment is not { Successful: true };

        // A client's own order has no composition until the pharmacist calculates the cost
        public bool HasComposition => Items.Count > 0 && Items.All(i => i.Components.Count > 0);

        // State diagram: the client cancels an order only before paying for it
        public bool CanClientCancel => Status is OrderStatus.Created or OrderStatus.AwaitingPayment;

        public void ChangeStatus(OrderStatus next)
        {
            if (!CanChangeTo(next))
                throw new InvalidOperationException(
                    $"Неможливий перехід зі стану «{Status.GetDisplayName()}» у стан «{next.GetDisplayName()}»");
            Status = next;
            StatusChangedAt = DateTime.Now;
        }

        // "Check the prescription" ends with "Reject the order" (extend) when the prescription is not valid:
        // expired, or the pharmacist finds a wrong dosage or incompatible components
        public void Reject(string reason)
        {
            if (Status == OrderStatus.Created) ChangeStatus(OrderStatus.UnderReview);
            ChangeStatus(OrderStatus.Rejected);
            StatusReason = reason;
        }

        // Time events of the state diagram
        public static readonly TimeSpan PaymentTimeout = TimeSpan.FromDays(3);   // Awaiting payment → after(3 days) → Cancelled
        public static readonly TimeSpan PickupTimeout = TimeSpan.FromDays(10);   // Ready → after(10 days) / dispose → Cancelled

        public DateTime? ExpiresAt => Status switch
        {
            OrderStatus.AwaitingPayment => StatusChangedAt + PaymentTimeout,
            OrderStatus.Ready => StatusChangedAt + PickupTimeout,
            _ => null
        };

        public bool IsExpired(DateTime now) => ExpiresAt <= now;

        public DateTime? IssuedAt => Status == OrderStatus.Issued ? StatusChangedAt : null;

        public Payment Pay(string method)
        {
            if (Status != OrderStatus.AwaitingPayment)
                throw new InvalidOperationException($"Замовлення у стані «{Status.GetDisplayName()}» не очікує оплати");
            var payment = new Payment { Amount = Total, Method = method, OrderId = Id };
            if (!payment.Process())
                throw new InvalidOperationException("Оплата не пройшла");
            Payment = payment;
            ChangeStatus(OrderStatus.Paid);
            return payment;
        }

        // Returns true when a payment was refunded (the "Cancelled" state: entry / refund if paid)
        public bool Cancel(string? reason = null)
        {
            ChangeStatus(OrderStatus.Cancelled);
            StatusReason = reason;
            if (Payment is not { Successful: true }) return false;
            Payment.Refund();
            return true;
        }
    }
}
