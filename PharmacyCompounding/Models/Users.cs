using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PharmacyCompounding.Models
{
    // Abstract User class, the base for Client and Employee
    public abstract class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(100)]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // The database stores a hash of the password (PBKDF2), not the password itself
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string? Password { get; set; }

        private static readonly PasswordHasher<User> Hasher = new();

        public void SetPassword(string password) => Password = Hasher.HashPassword(this, password);

        public bool Login(string email, string password) =>
            Password != null &&
            string.Equals(Email, email, StringComparison.OrdinalIgnoreCase) &&
            Hasher.VerifyHashedPassword(this, Password, password) != PasswordVerificationResult.Failed;

        // Role used for authorization; matches the discriminator value in the Users table
        public abstract string Role { get; }
    }

    public class Client : User
    {
        [Display(Name = "Адреса")]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата народження")]
        public DateTime? BirthDate { get; set; }

        public List<Order> Orders { get; set; } = new();

        public override string Role => Roles.Client;
    }

    // Abstract Employee class
    public abstract class Employee : User
    {
        [Display(Name = "Табельний номер")]
        public string PersonnelNumber { get; set; } = string.Empty;

        [Display(Name = "Посада")]
        public string Position { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Дата прийому")]
        public DateTime HireDate { get; set; }
    }

    // Pharmacist
    public class Pharmacist : Employee
    {
        [Display(Name = "Кваліфікаційна категорія")]
        public string QualificationCategory { get; set; } = string.Empty;

        public List<QualityCheck> QualityChecks { get; set; } = new();

        public override string Role => Roles.Pharmacist;
    }

    // PharmTechnologist
    public class PharmTechnologist : Employee
    {
        [Display(Name = "Допуск до виготовлення")]
        public bool AdmittedToProduction { get; set; }

        public List<ManufacturingAct> ManufacturingActs { get; set; } = new();

        public override string Role => Roles.Technologist;
    }

    // PharmacyManager: manages the staff and has full access to the system
    public class PharmacyManager : Employee
    {
        public override string Role => Roles.Manager;
    }

    public static class Roles
    {
        public const string Client = "Client";
        public const string Pharmacist = "Pharmacist";
        public const string Technologist = "Technologist";
        public const string Manager = "Manager";
    }
}
