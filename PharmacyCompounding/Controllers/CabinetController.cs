using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Client = await _db.Clients.FindAsync(ClientId);
            return View(await _db.Orders
                .Include(o => o.Prescription)
                .Include(o => o.Items)
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
