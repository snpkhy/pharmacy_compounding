using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // OrderItem: part of an Order (composition)
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Назва ліків")]
        public string MedicineName { get; set; } = string.Empty;

        [Display(Name = "Лікарська форма")]
        public string DosageForm { get; set; } = string.Empty;

        [Display(Name = "Дозування")]
        public string? Dosage { get; set; }

        [Display(Name = "Кількість")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Вартість роботи, грн")]
        public decimal WorkCost { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public List<Component> Components { get; set; } = new();

        public decimal CalculateCost() =>
            (WorkCost + Components.Sum(c => c.Cost())) * Quantity;
    }

    // Component: part of an order item, refers to an Ingredient
    public class Component
    {
        public int Id { get; set; }

        [Display(Name = "Кількість")]
        public double Amount { get; set; }

        [Display(Name = "Одиниця")]
        public string Unit { get; set; } = "г";

        public int OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }

        [Display(Name = "Інгредієнт")]
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }

        public decimal Cost() => (decimal)Amount * (Ingredient?.PricePerUnit ?? 0);
    }
}
