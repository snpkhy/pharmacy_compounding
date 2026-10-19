using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Orders controller: implements the order lifecycle from the state diagram (lab 1)
    public class OrdersController : Controller
    {
        private readonly PharmacyContext _db;

        public OrdersController(PharmacyContext db) => _db = db;

        // GET: /Orders?status=Ready&search=1002
        [HttpGet]
        public async Task<IActionResult> Index(OrderStatus? status, string? search)
        {
            var query = _db.Orders.Include(o => o.Client).Include(o => o.Prescription).AsQueryable();
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Number.ToString().Contains(search) || EF.Functions.ILike(o.Client!.FullName, $"%{search}%"));

            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.Counts = await _db.Orders.GroupBy(o => o.Status)
                                     .Select(g => new { g.Key, Count = g.Count() })
                                     .ToDictionaryAsync(x => x.Key, x => x.Count);
            return View(await query.OrderByDescending(o => o.Number).ToListAsync());
        }

        // GET: /Orders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await LoadOrder(id);
            if (order == null) return NotFound();
            ViewBag.Pharmacists = new SelectList(await _db.Pharmacists.ToListAsync(), "Id", "FullName");
            ViewBag.Technologists = new SelectList(await _db.Technologists.Where(t => t.AdmittedToProduction).ToListAsync(), "Id", "FullName");
            return View(order);
        }

        // GET: /Orders/Create — returns an empty form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillLists();
            return View(new OrderCreateViewModel());
        }

        // POST: /Orders/Create — accepts the form data and creates an order with its prescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            var components = vm.Components.Where(c => c.IngredientId.HasValue && c.Amount > 0).ToList();
            if (components.Count == 0)
                ModelState.AddModelError("", "Додайте хоча б один компонент");
            if (await _db.Prescriptions.AnyAsync(p => p.Number == vm.PrescriptionNumber))
                ModelState.AddModelError(nameof(vm.PrescriptionNumber), "Рецепт з таким номером уже використано");

            if (!ModelState.IsValid)
            {
                await FillLists();
                return View(vm);
            }

            var ingredients = await _db.Ingredients.ToDictionaryAsync(i => i.Id);
            var item = new OrderItem
            {
                MedicineName = vm.MedicineName,
                DosageForm = vm.DosageForm,
                Dosage = vm.Dosage,
                Quantity = vm.Quantity,
                WorkCost = vm.WorkCost,
                Components = components.Select(c => new Component
                {
                    IngredientId = c.IngredientId!.Value,
                    Ingredient = ingredients[c.IngredientId.Value],
                    Amount = c.Amount,
                    Unit = ingredients[c.IngredientId.Value].Unit
                }).ToList()
            };

            var order = new Order
            {
                Number = (await _db.Orders.MaxAsync(o => (int?)o.Number) ?? 1000) + 1,
                ClientId = vm.ClientId,
                PlannedReadyDate = vm.PlannedReadyDate,
                Prescription = new Prescription
                {
                    Number = vm.PrescriptionNumber,
                    DoctorId = vm.DoctorId,
                    IssueDate = vm.IssueDate,
                    ValidityDays = vm.ValidityDays,
                    Patient = vm.Patient,
                    Formula = vm.Formula
                }
            };
            order.AddItem(item);
            order.CalculateTotal();
            order.ChangeStatus(OrderStatus.UnderReview);   // prescription submitted → Under review

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Замовлення №{order.Number} створено та передано на перевірку рецепта";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // POST: /Orders/Verify/5 — the pharmacist verifies the prescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Verify(int id) => Transition(id, order =>
        {
            if (order.Prescription!.IsValid(DateTime.Today))
            {
                order.CalculateTotal();
                order.ChangeStatus(OrderStatus.AwaitingPayment);
                return $"Рецепт дійсний. Виставлено рахунок на {order.Total:0.00} грн";
            }
            order.ChangeStatus(OrderStatus.Rejected);
            return "Рецепт недійсний (минув термін дії). Замовлення відхилено";
        });

        // POST: /Orders/Pay/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Pay(int id, string method) => Transition(id, order =>
        {
            var payment = new Payment { Amount = order.Total, Method = method, OrderId = order.Id };
            if (!payment.Process())
                throw new InvalidOperationException("Оплата не пройшла");
            order.Payment = payment;
            order.ChangeStatus(OrderStatus.Paid);
            return $"Оплату {payment.Amount:0.00} грн отримано ({method})";
        });

        // POST: /Orders/Manufacture/5 — check stock, write off ingredients, create a manufacturing act
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manufacture(int id, int technologistId)
        {
            if (!await _db.Technologists.AnyAsync(t => t.Id == technologistId && t.AdmittedToProduction))
                return await Fail(id, "Оберіть фармацевта-технолога з допуском до виготовлення");
            return await Transition(id, order => ManufactureOrder(order, technologistId));
        }

        private static string ManufactureOrder(Order order, int technologistId)
        {
            var needed = order.Items.SelectMany(i => i.Components.Select(c => (c.Ingredient!, c.Amount * i.Quantity)))
                                    .GroupBy(x => x.Item1)
                                    .Select(g => (Ingredient: g.Key, Amount: g.Sum(x => x.Item2)))
                                    .ToList();
            var missing = needed.Where(n => !n.Ingredient.IsSufficient(n.Amount)).ToList();
            if (missing.Any())
            {
                if (order.Status != OrderStatus.AwaitingIngredients)
                    order.ChangeStatus(OrderStatus.AwaitingIngredients);
                return "Недостатньо інгредієнтів: " + string.Join(", ", missing.Select(m => m.Ingredient.Name)) +
                       ". Сформовано заявку на поставку";
            }

            if (order.Status == OrderStatus.AwaitingIngredients)
                order.ChangeStatus(OrderStatus.Paid);          // ingredients have arrived
            if (order.Status == OrderStatus.Paid)
                order.ChangeStatus(OrderStatus.InProduction);
            else if (!IsRemakeAllowed(order))
                throw new InvalidOperationException("Ліки вже виготовлено — очікується контроль якості");

            foreach (var n in needed) n.Ingredient.WriteOff(n.Amount);
            var actNo = order.ManufacturingActs.Count + 1;
            order.ManufacturingActs.Add(new ManufacturingAct
            {
                Number = order.Number * 10 + actNo,
                Batch = $"S-{order.Number}-{actNo}",
                TechnologistId = technologistId,
                Notes = actNo == 1 ? "Виготовлено за прописом" : "Повторне виготовлення після браку"
            });
            return $"Ліки виготовлено, інгредієнти списано, складено акт (серія S-{order.Number}-{actNo})";
        }

        // POST: /Orders/QualityControl/5 — the pharmacist performs quality control
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QualityControl(int id, int pharmacistId, bool organoleptic, bool physical, bool chemical)
        {
            if (!await _db.Pharmacists.AnyAsync(p => p.Id == pharmacistId))
                return await Fail(id, "Оберіть провізора, який проводить контроль");
            return await Transition(id, order =>
            {
                var act = order.ManufacturingActs.OrderByDescending(a => a.Id).FirstOrDefault();
                if (order.Status != OrderStatus.InProduction || act == null || act.QualityCheck != null)
                    throw new InvalidOperationException("Немає виготовленої серії для контролю");

                order.ChangeStatus(OrderStatus.QualityControl);
                var check = new QualityCheck
                {
                    PharmacistId = pharmacistId,
                    Organoleptic = organoleptic,
                    Physical = physical,
                    Chemical = chemical
                };
                act.QualityCheck = check;
                if (check.Approve())
                {
                    order.ChangeStatus(OrderStatus.Ready);
                    return "Контроль якості пройдено. Ліки промарковано та упаковано, клієнта сповіщено";
                }
                order.ChangeStatus(OrderStatus.InProduction);
                return $"Контроль якості не пройдено — серію {act.Batch} забраковано, потрібне повторне виготовлення";
            });
        }

        // POST: /Orders/Issue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Issue(int id) => Transition(id, order =>
        {
            order.ChangeStatus(OrderStatus.Issued);
            return "Ліки видано клієнту";
        });

        // POST: /Orders/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Cancel(int id) => Transition(id, order =>
        {
            order.ChangeStatus(OrderStatus.Cancelled);
            if (order.Payment is { Successful: true })
            {
                order.Payment.Successful = false;
                return $"Замовлення скасовано, кошти ({order.Payment.Amount:0.00} грн) повернуто";
            }
            return "Замовлення скасовано";
        });

        // Shared logic for POST actions: load, change state, save, return to the details page
        private async Task<IActionResult> Transition(int id, Func<Order, string> action)
        {
            var order = await LoadOrder(id);
            if (order == null) return NotFound();
            try
            {
                TempData["Message"] = action(order);
                await _db.SaveChangesAsync();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        private Task<IActionResult> Fail(int id, string error)
        {
            TempData["Error"] = error;
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Details), new { id }));
        }

        private static bool IsRemakeAllowed(Order order) =>
            order.Status == OrderStatus.InProduction &&
            order.ManufacturingActs.OrderByDescending(a => a.Id).FirstOrDefault()?.QualityCheck is { Result: false };

        private Task<Order?> LoadOrder(int id) =>
            _db.Orders
               .Include(o => o.Client)
               .Include(o => o.Prescription).ThenInclude(p => p!.Doctor)
               .Include(o => o.Items).ThenInclude(i => i.Components).ThenInclude(c => c.Ingredient)
               .Include(o => o.Payment)
               .Include(o => o.ManufacturingActs).ThenInclude(a => a.Technologist)
               .Include(o => o.ManufacturingActs).ThenInclude(a => a.QualityCheck).ThenInclude(q => q!.Pharmacist)
               .AsSplitQuery()
               .FirstOrDefaultAsync(o => o.Id == id);

        private async Task FillLists()
        {
            ViewBag.Clients = new SelectList(await _db.Clients.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.Doctors = new SelectList(await _db.Doctors.OrderBy(d => d.FullName).ToListAsync(), "Id", "FullName");
            ViewBag.Ingredients = new SelectList(
                (await _db.Ingredients.OrderBy(i => i.Name).ToListAsync())
                    .Select(i => new { i.Id, Title = $"{i.Name} ({i.PricePerUnit:0.00} грн/{i.Unit}, залишок {i.Stock:0.##})" }),
                "Id", "Title");
        }
    }
}
