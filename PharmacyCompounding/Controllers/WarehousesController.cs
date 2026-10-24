using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    // Warehouses (CRUD): a warehouse aggregates ingredients
    public class WarehousesController : Controller
    {
        private readonly PharmacyContext _db;

        public WarehousesController(PharmacyContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index() =>
            View(await _db.Warehouses.Include(w => w.Ingredients).OrderBy(w => w.Name).ToListAsync());

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public IActionResult Create() => View(new Warehouse());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            await ValidateName(warehouse);
            if (!ModelState.IsValid) return View(warehouse);
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Склад «{warehouse.Name}» додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Edit(int id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id);
            return warehouse == null ? NotFound() : View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Edit(int id, Warehouse warehouse)
        {
            if (id != warehouse.Id) return BadRequest();
            await ValidateName(warehouse);
            if (!ModelState.IsValid) return View(warehouse);
            _db.Update(warehouse);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Зміни для складу «{warehouse.Name}» збережено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _db.Warehouses.Include(w => w.Ingredients).FirstOrDefaultAsync(w => w.Id == id);
            return warehouse == null ? NotFound() : View(warehouse);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();
            // Ingredients refer to the warehouse with ON DELETE RESTRICT
            if (await _db.Ingredients.AnyAsync(i => i.WarehouseId == id))
            {
                TempData["Error"] = $"На складі «{warehouse.Name}» є інгредієнти — перемістіть або видаліть їх";
                return RedirectToAction(nameof(Index));
            }
            _db.Warehouses.Remove(warehouse);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Склад «{warehouse.Name}» видалено";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateName(Warehouse warehouse)
        {
            if (!string.IsNullOrWhiteSpace(warehouse.Name) &&
                await _db.Warehouses.AnyAsync(w => w.Id != warehouse.Id && w.Name.ToLower() == warehouse.Name.Trim().ToLower()))
                ModelState.AddModelError(nameof(Warehouse.Name), "Склад з такою назвою вже існує");
        }
    }
}
