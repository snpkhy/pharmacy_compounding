using System.ComponentModel.DataAnnotations;
using PharmacyCompounding.Models;

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

    // Order edit form: the same fields as the new order form plus the order identity
    public class OrderEditViewModel : OrderCreateViewModel
    {
        public int Id { get; set; }

        public int Number { get; set; }

        // Row version of the order when the form was opened (optimistic concurrency)
        public uint Version { get; set; }

        public static OrderEditViewModel From(Order order)
        {
            var item = order.Items.First();
            var prescription = order.Prescription!;
            var components = item.Components
                .Select(c => new ComponentInput { IngredientId = c.IngredientId, Amount = c.Amount })
                .ToList();
            // Spare empty rows to add new components
            components.AddRange(Enumerable.Range(0, 2).Select(_ => new ComponentInput()));

            return new OrderEditViewModel
            {
                Id = order.Id, Number = order.Number, Version = order.Version, ClientId = order.ClientId, PlannedReadyDate = order.PlannedReadyDate,
                DoctorId = prescription.DoctorId, PrescriptionNumber = prescription.Number, IssueDate = prescription.IssueDate,
                ValidityDays = prescription.ValidityDays, Patient = prescription.Patient, Formula = prescription.Formula,
                MedicineName = item.MedicineName, DosageForm = item.DosageForm, Dosage = item.Dosage,
                Quantity = item.Quantity, WorkCost = item.WorkCost, Components = components
            };
        }

        public void ApplyTo(Order order, IReadOnlyDictionary<int, Ingredient> ingredients)
        {
            order.ClientId = ClientId;
            order.PlannedReadyDate = PlannedReadyDate;

            var prescription = order.Prescription!;
            prescription.DoctorId = DoctorId;
            prescription.Number = PrescriptionNumber.Trim();
            prescription.IssueDate = IssueDate;
            prescription.ValidityDays = ValidityDays;
            prescription.Patient = Patient;
            prescription.Formula = Formula;

            var item = order.Items.First();
            item.MedicineName = MedicineName;
            item.DosageForm = DosageForm;
            item.Dosage = Dosage;
            item.Quantity = Quantity;
            item.WorkCost = WorkCost;
            // The composition is replaced: removed components are deleted by EF Core (orphans of a composition)
            item.Components.Clear();
            item.Components.AddRange(Components
                .Where(c => c.IngredientId.HasValue && c.Amount > 0)
                .Select(c => new Component
                {
                    IngredientId = c.IngredientId!.Value,
                    Ingredient = ingredients[c.IngredientId.Value],
                    Amount = c.Amount,
                    Unit = ingredients[c.IngredientId.Value].Unit
                }));

            order.CalculateTotal();
        }
    }

    public class ComponentInput
    {
        public int? IngredientId { get; set; }

        [Range(0, 100000)]
        public double Amount { get; set; }
    }
}
