using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Ingredient
    public class Ingredient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть код")]
        [StringLength(20)]
        [Display(Name = "Код")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть назву")]
        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Одиниця виміру")]
        public string Unit { get; set; } = "г";

        [Range(0, 100000)]
        [Display(Name = "Ціна за одиницю, грн")]
        public decimal PricePerUnit { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Залишок")]
        public double Stock { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Мінімальний залишок")]
        public double MinStock { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Термін придатності")]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);

        [Display(Name = "Склад")]
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public bool IsSufficient(double amount) => Stock >= amount;

        public bool IsLow => Stock < MinStock;

        public void WriteOff(double amount)
        {
            if (!IsSufficient(amount))
                throw new InvalidOperationException($"Недостатньо інгредієнта «{Name}»");
            Stock -= amount;
        }

        public void Replenish(double amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Stock += amount;
        }
    }
}
