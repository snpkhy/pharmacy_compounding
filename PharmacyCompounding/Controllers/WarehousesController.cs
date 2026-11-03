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
        public IActionResult Create() => View(new Warehouse());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address")] Warehouse warehouse)
        {
            await ValidateName(warehouse);
            if (!ModelState.IsValid) return View(warehouse);
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Склад «{warehouse.Name}» додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id);
            return warehouse == null ? NotFound() : View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address")] Warehouse warehouse)
        {
            if (id != warehouse.Id) return BadRequest();
            await ValidateName(warehouse);
            if (!ModelState.IsValid) return View(warehouse);
            var existing = await _db.Warehouses.FindAsync(id);
            if (existing == null) return NotFound();
            existing.Name = warehouse.Name;
            existing.Address = warehouse.Address;
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Зміни для складу «{existing.Name}» збережено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _db.Warehouses.Include(w => w.Ingredients).FirstOrDefaultAsync(w => w.Id == id);
            return warehouse == null ? NotFound() : View(warehouse);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        // The name is stored trimmed, so both sides of the comparison are normalized the same way
        private async Task ValidateName(Warehouse warehouse)
        {
            warehouse.Name = warehouse.Name?.Trim() ?? string.Empty;
            var name = warehouse.Name.ToLower();
            if (name.Length > 0 &&
                await _db.Warehouses.AnyAsync(w => w.Id != warehouse.Id && w.Name.Trim().ToLower() == name))
                ModelState.AddModelError(nameof(Warehouse.Name), "Склад з такою назвою вже існує");
        }
    }
}
