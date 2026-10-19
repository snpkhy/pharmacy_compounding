using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    public class ClientsController : Controller
    {
        private readonly PharmacyContext _db;

        public ClientsController(PharmacyContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index() =>
            View(await _db.Clients.Include(c => c.Orders).OrderBy(c => c.FullName).ToListAsync());

        [HttpGet]
        public IActionResult Create() => View(new Client());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            await ValidateEmail(client);
            if (!ModelState.IsValid) return View(client);
            if (!string.IsNullOrEmpty(client.Password)) client.SetPassword(client.Password);
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Клієнта «{client.FullName}» зареєстровано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _db.Clients.FindAsync(id);
            return client == null ? NotFound() : View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id) return BadRequest();
            await ValidateEmail(client);
            if (!ModelState.IsValid) return View(client);

            // An empty password field keeps the current password
            var oldHash = await _db.Clients.Where(c => c.Id == id).Select(c => c.Password).FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(client.Password)) client.Password = oldHash;
            else client.SetPassword(client.Password);
            _db.Update(client);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Дані клієнта оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _db.Clients.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound();
            if (client.Orders.Any())
                TempData["Error"] = "Неможливо видалити клієнта, який має замовлення";
            else
            {
                _db.Clients.Remove(client);
                await _db.SaveChangesAsync();
                TempData["Message"] = "Клієнта видалено";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateEmail(Client client)
        {
            if (!string.IsNullOrWhiteSpace(client.Email) &&
                await _db.Users.AnyAsync(u => u.Id != client.Id && u.Email!.ToLower() == client.Email.ToLower()))
                ModelState.AddModelError(nameof(client.Email), "Користувач з таким email уже зареєстрований");
        }
    }
}
