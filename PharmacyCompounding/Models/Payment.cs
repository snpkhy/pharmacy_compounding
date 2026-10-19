using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // Payment
    public class Payment
    {
        public int Id { get; set; }

        [Display(Name = "Сума, грн")]
        public decimal Amount { get; set; }

        [Display(Name = "Дата")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Спосіб оплати")]
        public string Method { get; set; } = "Картка";

        [Display(Name = "Успішна")]
        public bool Successful { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public bool Process()
        {
            Successful = Amount > 0;
            return Successful;
        }
    }
}
