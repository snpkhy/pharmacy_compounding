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
    }
}
