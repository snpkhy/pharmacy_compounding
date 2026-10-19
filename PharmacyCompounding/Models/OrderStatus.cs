using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // OrderStatus enumeration from the class diagram (lab 1)
    public enum OrderStatus
    {
        [Display(Name = "Створене")] Created,
        [Display(Name = "На перевірці")] UnderReview,
        [Display(Name = "Відхилене")] Rejected,
        [Display(Name = "Очікує оплати")] AwaitingPayment,
        [Display(Name = "Оплачене")] Paid,
        [Display(Name = "Очікує інгредієнтів")] AwaitingIngredients,
        [Display(Name = "У виготовленні")] InProduction,
        [Display(Name = "На контролі якості")] QualityControl,
        [Display(Name = "Готове")] Ready,
        [Display(Name = "Видане")] Issued,
        [Display(Name = "Скасоване")] Cancelled
    }
}
