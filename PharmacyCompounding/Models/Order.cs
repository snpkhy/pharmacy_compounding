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

        public void ChangeStatus(OrderStatus next)
        {
            if (!CanChangeTo(next))
                throw new InvalidOperationException(
                    $"Неможливий перехід зі стану «{Status.GetDisplayName()}» у стан «{next.GetDisplayName()}»");
            Status = next;
        }
    }
}
