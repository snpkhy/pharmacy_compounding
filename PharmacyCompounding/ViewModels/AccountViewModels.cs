using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.ViewModels
{
    // Sign-in form data
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Вкажіть email")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запам'ятати мене")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    // Client sign-up form data
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(100)]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть email")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Некоректний номер телефону")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Вкажіть пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторіть пароль")]
        [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
        [DataType(DataType.Password)]
        [Display(Name = "Повторіть пароль")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // The client's own profile: personal data and an optional password change
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Вкажіть ПІБ")]
        [StringLength(100)]
        [Display(Name = "ПІБ")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Некоректний номер телефону")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Display(Name = "Адреса")]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата народження")]
        public DateTime? BirthDate { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Поточний пароль")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        [DataType(DataType.Password)]
        [Display(Name = "Новий пароль")]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "Паролі не збігаються")]
        [DataType(DataType.Password)]
        [Display(Name = "Повторіть новий пароль")]
        public string? ConfirmNewPassword { get; set; }
    }
}
