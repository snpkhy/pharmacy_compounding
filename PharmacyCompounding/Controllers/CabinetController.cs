using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Client's personal account: only their own orders, no staff actions
    [Authorize(Roles = Roles.Client)]
    public class CabinetController : Controller
    {
        private readonly PharmacyContext _db;

        public CabinetController(PharmacyContext db) => _db = db;

        private int ClientId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Cash is paid at the counter, so online the client pays by card only
        public static readonly string[] OnlinePaymentMethods = { "Картка", "Apple Pay / Google Pay" };

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Client = await _db.Clients.FindAsync(ClientId);
            return View(await _db.Orders
                .Include(o => o.Prescription)
                .Include(o => o.Items).ThenInclude(i => i.Components)
                .AsSplitQuery()
                .Where(o => o.ClientId == ClientId)
                .OrderByDescending(o => o.Number)
                .ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Prescription).ThenInclude(p => p!.Doctor)
                .Include(o => o.Items).ThenInclude(i => i.Components).ThenInclude(c => c.Ingredient)
                .Include(o => o.Payment)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == ClientId);
            return order == null ? NotFound() : View(order);
        }

        // GET: /Cabinet/Create — "Place an order" with "Add a doctor's prescription" (use case diagram, lab 1)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = await _db.Clients.FindAsync(ClientId);
            await FillDoctors();
            return View(new ClientOrderViewModel { Patient = client?.FullName ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientOrderViewModel vm)
        {
            var client = await _db.Clients.FindAsync(ClientId);
            if (client == null) return NotFound();
            vm.PrescriptionNumber = vm.PrescriptionNumber?.Trim() ?? string.Empty;
            if (!await _db.Doctors.AnyAsync(d => d.Id == vm.DoctorId))
                ModelState.AddModelError(nameof(vm.DoctorId), "Оберіть лікаря зі списку");
            if (await _db.Prescriptions.AnyAsync(p => p.Number == vm.PrescriptionNumber))
                ModelState.AddModelError(nameof(vm.PrescriptionNumber), "Рецепт з таким номером уже використано");
            if (!ModelState.IsValid)
            {
                await FillDoctors();
                return View(vm);
            }

            var order = client.CreateOrder(vm.ToPrescription(), vm.ToItem());
            // One SaveChanges: the prescription, the order and its item are inserted in one transaction
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Замовлення №{order.Number} оформлено. Провізор перевірить рецепт і розрахує вартість — " +
                                  "після цього замовлення можна буде оплатити тут";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // POST: /Cabinet/Pay/5 — "Pay for the order" through the payment system
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Pay(int id, string method) => Transition(id, order =>
        {
            var payment = order.Pay(OnlinePaymentMethods.Contains(method) ? method : OnlinePaymentMethods[0]);
            return $"Оплату {payment.Amount:0.00} грн отримано. Ліки передано на виготовлення";
        });

        // POST: /Cabinet/Cancel/5 — "Cancel the order" (only before payment)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Cancel(int id) => Transition(id, order =>
        {
            if (!order.CanClientCancel)
                throw new InvalidOperationException(
                    $"Замовлення у стані «{order.Status.GetDisplayName()}» скасувати онлайн не можна. Зверніться до аптеки");
            order.Cancel("Скасовано клієнтом");
            return "Замовлення скасовано";
        });

        // Loads only the current client's order, applies the action and saves it in one transaction
        private async Task<IActionResult> Transition(int id, Func<Order, string> action)
        {
            var order = await _db.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == ClientId);
            if (order == null) return NotFound();
            try
            {
                TempData["Message"] = action(order);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData.Remove("Message");
                TempData["Error"] = "Замовлення щойно змінила аптека. Перегляньте актуальний стан і повторіть дію";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task FillDoctors() =>
            ViewBag.Doctors = (await _db.Doctors.OrderBy(d => d.FullName).ToListAsync())
                .Select(d => new SelectListItem(
                    string.IsNullOrEmpty(d.Specialization) ? d.FullName : $"{d.FullName} — {d.Specialization}",
                    d.Id.ToString()))
                .ToList();

        // GET: /Cabinet/Profile — the client edits their own data
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var client = await _db.Clients.FindAsync(ClientId);
            if (client == null) return NotFound();
            return View(new ProfileViewModel
            {
                FullName = client.FullName, Email = client.Email, Phone = client.Phone,
                Address = client.Address, BirthDate = client.BirthDate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel vm)
        {
            var client = await _db.Clients.FindAsync(ClientId);
            if (client == null) return NotFound();
            vm.Email = client.Email;
            // A new password is accepted only together with the current one
            if (!string.IsNullOrEmpty(vm.NewPassword) && !client.Login(client.Email ?? "", vm.CurrentPassword ?? ""))
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Невірний поточний пароль");
            if (!ModelState.IsValid) return View(vm);

            client.FullName = vm.FullName.Trim();
            client.Phone = vm.Phone;
            client.Address = vm.Address;
            client.BirthDate = vm.BirthDate;
            if (!string.IsNullOrEmpty(vm.NewPassword)) client.SetPassword(vm.NewPassword);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Профіль оновлено. Нове ім'я з'явиться в меню після наступного входу";
            return RedirectToAction(nameof(Profile));
        }
    }
}
