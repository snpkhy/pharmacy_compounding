using System.ComponentModel.DataAnnotations;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.ViewModels
{
    // One form for all Employee subclasses; the type decides which class is created
    public class EmployeeFormViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Роль")]
        public string Type { get; set; } = Roles.Pharmacist;

        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(100)]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Вкажіть email — він потрібен для входу")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Вкажіть табельний номер")]
        [Display(Name = "Табельний номер")]
        public string PersonnelNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть посаду")]
        [Display(Name = "Посада")]
        public string Position { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Дата прийому")]
        public DateTime HireDate { get; set; } = DateTime.Today;

        [Display(Name = "Кваліфікаційна категорія")]
        public string? QualificationCategory { get; set; }

        [Display(Name = "Допуск до виготовлення")]
        public bool AdmittedToProduction { get; set; }

        public static readonly Dictionary<string, string> Types = new()
        {
            [Roles.Pharmacist] = "Провізор",
            [Roles.Technologist] = "Фармацевт-технолог",
            [Roles.Manager] = "Завідувач аптеки"
        };

        public static EmployeeFormViewModel From(Employee e) => new()
        {
            Id = e.Id, Type = e.Role, FullName = e.FullName, Phone = e.Phone, Email = e.Email ?? "",
            PersonnelNumber = e.PersonnelNumber, Position = e.Position, HireDate = e.HireDate,
            QualificationCategory = (e as Pharmacist)?.QualificationCategory,
            AdmittedToProduction = (e as PharmTechnologist)?.AdmittedToProduction ?? false
        };

        public Employee CreateEmployee() => Type switch
        {
            Roles.Technologist => new PharmTechnologist(),
            Roles.Manager => new PharmacyManager(),
            _ => new Pharmacist()
        };

        public void ApplyTo(Employee e)
        {
            e.FullName = FullName.Trim();
            e.Phone = Phone;
            e.Email = Email.Trim();
            e.PersonnelNumber = PersonnelNumber;
            e.Position = Position;
            e.HireDate = HireDate;
            if (e is Pharmacist p) p.QualificationCategory = QualificationCategory ?? "";
            if (e is PharmTechnologist t) t.AdmittedToProduction = AdmittedToProduction;
            if (!string.IsNullOrEmpty(Password)) e.SetPassword(Password);
        }
    }
}
