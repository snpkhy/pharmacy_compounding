using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Doctor
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Спеціалізація")]
        public string? Specialization { get; set; }

        [Display(Name = "Заклад охорони здоров'я")]
        public string? Institution { get; set; }

        [Required(ErrorMessage = "Вкажіть номер ліцензії")]
        [Display(Name = "Номер ліцензії")]
        public string LicenseNumber { get; set; } = string.Empty;

        public List<Prescription> Prescriptions { get; set; } = new();
    }
}
