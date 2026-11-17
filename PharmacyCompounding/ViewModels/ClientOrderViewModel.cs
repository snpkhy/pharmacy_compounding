using System.ComponentModel.DataAnnotations;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.ViewModels
{
    // The client's own order form: the doctor's prescription and what to make.
    // The composition (ingredients) and the cost are left to the pharmacist, who checks the prescription.
    public class ClientOrderViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Оберіть лікаря")]
        [Display(Name = "Лікар, який виписав рецепт")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Вкажіть номер рецепта")]
        [StringLength(50)]
        [Display(Name = "Номер рецепта")]
        public string PrescriptionNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Дата виписки")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Range(1, 365, ErrorMessage = "Термін дії: від 1 до 365 днів")]
        [Display(Name = "Термін дії (днів)")]
        public int ValidityDays { get; set; } = 30;

        [Required(ErrorMessage = "Вкажіть пацієнта")]
        [StringLength(100)]
        [Display(Name = "Пацієнт")]
        public string Patient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Перепишіть пропис з рецепта")]
        [Display(Name = "Пропис (Rp.)")]
        public string Formula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть назву ліків")]
        [StringLength(100)]
        [Display(Name = "Назва ліків")]
        public string MedicineName { get; set; } = string.Empty;

        [Display(Name = "Лікарська форма")]
        public string DosageForm { get; set; } = "Мазь";

        [Display(Name = "Дозування / об'єм")]
        public string? Dosage { get; set; }

        [Range(1, 20, ErrorMessage = "Кількість: від 1 до 20")]
        [Display(Name = "Кількість")]
        public int Quantity { get; set; } = 1;

        public Prescription ToPrescription() => new()
        {
            Number = PrescriptionNumber.Trim(),
            DoctorId = DoctorId,
            IssueDate = IssueDate,
            ValidityDays = ValidityDays,
            Patient = Patient.Trim(),
            Formula = Formula.Trim()
        };

        public OrderItem ToItem() => new()
        {
            MedicineName = MedicineName.Trim(),
            DosageForm = DosageForm,
            Dosage = Dosage,
            Quantity = Quantity
        };
    }
}
