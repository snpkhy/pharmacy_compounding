using System.ComponentModel.DataAnnotations;

namespace PharmacyCompounding.Models
{
    // ManufacturingAct
    public class ManufacturingAct
    {
        public int Id { get; set; }

        [Display(Name = "Номер акта")]
        public int Number { get; set; }

        [Display(Name = "Дата виготовлення")]
        public DateTime ManufacturedAt { get; set; } = DateTime.Now;

        [Display(Name = "Серія")]
        public string Batch { get; set; } = string.Empty;

        [Display(Name = "Примітки")]
        public string? Notes { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Display(Name = "Фармацевт-технолог")]
        public int TechnologistId { get; set; }
        public PharmTechnologist? Technologist { get; set; }

        public QualityCheck? QualityCheck { get; set; }
    }

    // QualityCheck
    public class QualityCheck
    {
        public int Id { get; set; }

        [Display(Name = "Дата")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Органолептичний")]
        public bool Organoleptic { get; set; }

        [Display(Name = "Фізичний")]
        public bool Physical { get; set; }

        [Display(Name = "Хімічний")]
        public bool Chemical { get; set; }

        [Display(Name = "Результат")]
        public bool Result { get; set; }

        public int ManufacturingActId { get; set; }
        public ManufacturingAct? ManufacturingAct { get; set; }

        [Display(Name = "Провізор")]
        public int PharmacistId { get; set; }
        public Pharmacist? Pharmacist { get; set; }

        public bool Approve()
        {
            Result = Organoleptic && Physical && Chemical;
            return Result;
        }
    }
}
