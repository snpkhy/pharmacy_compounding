using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Warehouse: aggregates ingredients
    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Назва")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Адреса")]
        public string? Address { get; set; }

        public List<Ingredient> Ingredients { get; set; } = new();

        public bool CheckAvailability(Ingredient ingredient, double amount) =>
            Ingredients.Contains(ingredient) && ingredient.IsSufficient(amount);

        // Class diagram: Warehouse.createSupplyRequest(). Orders enough of each ingredient to cover the paid orders
        // that still wait for manufacturing (reserved) and to bring the stock back up to the minimum
        public List<SupplyLine> CreateSupplyRequest(IReadOnlyDictionary<int, double> reserved) =>
            Ingredients
                .Select(i => new SupplyLine(i, reserved.GetValueOrDefault(i.Id),
                    Math.Ceiling(Math.Max(0, i.MinStock + reserved.GetValueOrDefault(i.Id) - i.Stock))))
                .Where(l => l.ToOrder > 0)
                .OrderBy(l => l.Ingredient.Code)
                .ToList();
    }

    // One line of a supply request
    public record SupplyLine(Ingredient Ingredient, double Reserved, double ToOrder)
    {
        public decimal Cost => (decimal)ToOrder * Ingredient.PricePerUnit;
    }
}
