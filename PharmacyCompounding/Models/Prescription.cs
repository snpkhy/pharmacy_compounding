using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Prescription
    public class Prescription
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть номер рецепта")]
        [Display(Name = "Номер рецепта")]
        public string Number { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Дата виписки")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Range(1, 365, ErrorMessage = "Термін дії: від 1 до 365 днів")]
        [Display(Name = "Термін дії (днів)")]
        public int ValidityDays { get; set; } = 30;

        [Required(ErrorMessage = "Вкажіть пацієнта")]
        [Display(Name = "Пацієнт")]
        public string Patient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть пропис")]
        [Display(Name = "Пропис")]
        public string Formula { get; set; } = string.Empty;

        [Display(Name = "Лікар")]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public Order? Order { get; set; }

        public bool IsValid(DateTime onDate) =>
            onDate.Date <= IssueDate.Date.AddDays(ValidityDays);
    }
}
