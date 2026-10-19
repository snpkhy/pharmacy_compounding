using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.ViewModels
{
    // New order form data (client + prescription + item with components)
    public class OrderCreateViewModel
    {
        [Display(Name = "Клієнт")]
        public int ClientId { get; set; }

        [Display(Name = "Лікар")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Вкажіть номер рецепта")]
        [Display(Name = "Номер рецепта")]
        public string PrescriptionNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Дата виписки")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Range(1, 365)]
        [Display(Name = "Термін дії (днів)")]
        public int ValidityDays { get; set; } = 30;

        [Required(ErrorMessage = "Вкажіть пацієнта")]
        [Display(Name = "Пацієнт")]
        public string Patient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть пропис")]
        [Display(Name = "Пропис (Rp.)")]
        public string Formula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть назву ліків")]
        [Display(Name = "Назва ліків")]
        public string MedicineName { get; set; } = string.Empty;

        [Display(Name = "Лікарська форма")]
        public string DosageForm { get; set; } = "Мазь";

        [Display(Name = "Дозування / об'єм")]
        public string? Dosage { get; set; }

        [Range(1, 20)]
        [Display(Name = "Кількість")]
        public int Quantity { get; set; } = 1;

        [Range(0, 10000)]
        [Display(Name = "Вартість роботи, грн")]
        public decimal WorkCost { get; set; } = 60;

        [DataType(DataType.Date)]
        [Display(Name = "Планова дата готовності")]
        public DateTime PlannedReadyDate { get; set; } = DateTime.Today.AddDays(3);

        public List<ComponentInput> Components { get; set; } =
            Enumerable.Range(0, 3).Select(_ => new ComponentInput()).ToList();
    }

    public class ComponentInput
    {
        public int? IngredientId { get; set; }

        [Range(0, 100000)]
        public double Amount { get; set; }
    }
}
