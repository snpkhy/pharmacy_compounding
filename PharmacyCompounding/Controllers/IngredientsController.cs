using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    // Ingredient catalog and warehouse stock (CRUD)
    public class IngredientsController : Controller
    {
        private readonly PharmacyContext _db;

        public IngredientsController(PharmacyContext db) => _db = db;

        // GET: /Ingredients?search=...&lowOnly=true&warehouseId=2
        [HttpGet]
        public async Task<IActionResult> Index(string? search, bool lowOnly = false, int? warehouseId = null)
        {
            var query = _db.Ingredients.Include(i => i.Warehouse).AsQueryable();
            if (warehouseId.HasValue)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => EF.Functions.ILike(i.Name, $"%{search}%") || EF.Functions.ILike(i.Code, $"%{search}%"));
            if (lowOnly)
                query = query.Where(i => i.Stock < i.MinStock);

            ViewBag.Search = search;
            ViewBag.LowOnly = lowOnly;
            ViewBag.WarehouseId = warehouseId;
            ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name", warehouseId);
            return View(await query.OrderBy(i => i.Code).ToListAsync());
        }

        [HttpGet]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Create()
        {
            await FillWarehouses();
            return View(new Ingredient());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Create(Ingredient ingredient)
        {
            if (await _db.Ingredients.AnyAsync(i => i.Code == ingredient.Code))
                ModelState.AddModelError(nameof(Ingredient.Code), "Інгредієнт з таким кодом уже існує");
            if (!ModelState.IsValid)
            {
                await FillWarehouses();
                return View(ingredient);
            }
            _db.Ingredients.Add(ingredient);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Інгредієнт «{ingredient.Name}» додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Edit(int id)
        {
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            await FillWarehouses();
            return View(ingredient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Edit(int id, Ingredient ingredient)
        {
            if (id != ingredient.Id) return BadRequest();
            if (await _db.Ingredients.AnyAsync(i => i.Code == ingredient.Code && i.Id != id))
                ModelState.AddModelError(nameof(Ingredient.Code), "Інгредієнт з таким кодом уже існує");
            if (!ModelState.IsValid)
            {
                await FillWarehouses();
                return View(ingredient);
            }
            // Update() sends "WHERE id = @id AND xmin = @Version": the version comes from the hidden form field
            _db.Update(ingredient);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var current = (Ingredient?)(await ex.Entries.Single().GetDatabaseValuesAsync())?.ToObject();
                if (current == null)
                {
                    TempData["Error"] = "Інгредієнт уже видалив інший користувач";
                    return RedirectToAction(nameof(Index));
                }
                // Show the current values and take the new version, so saving again overwrites them on purpose
                ModelState.AddModelError("", $"Поки ви редагували, запис змінив інший користувач. Зараз у базі: " +
                    $"ціна {current.PricePerUnit:0.00} грн, залишок {current.Stock:0.##} {current.Unit}, мінімум {current.MinStock:0.##}. " +
                    "Перевірте дані й збережіть ще раз, щоб записати свої значення");
                ModelState.Remove(nameof(Ingredient.Version));
                ingredient.Version = current.Version;
                await FillWarehouses();
                return View(ingredient);
            }
            TempData["Message"] = $"Зміни для «{ingredient.Name}» збережено";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Ingredients/Replenish/5 — restock an ingredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Replenish(int id, double amount)
        {
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            try
            {
                ingredient.Replenish(amount);
                await _db.SaveChangesAsync();
                TempData["Message"] = $"«{ingredient.Name}» поповнено на {amount} {ingredient.Unit}";
            }
            catch (ArgumentOutOfRangeException)
            {
                TempData["Error"] = "Кількість для поповнення має бути більшою за нуль";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = $"Залишок «{ingredient.Name}» щойно змінив інший користувач. Повторіть поповнення";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _db.Ingredients.Include(i => i.Warehouse).FirstOrDefaultAsync(i => i.Id == id);
            return ingredient == null ? NotFound() : View(ingredient);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            if (await _db.Components.AnyAsync(c => c.IngredientId == id))
            {
                TempData["Error"] = $"«{ingredient.Name}» використовується у замовленнях і не може бути видалений";
                return RedirectToAction(nameof(Index));
            }
            _db.Ingredients.Remove(ingredient);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Інгредієнт «{ingredient.Name}» видалено";
            return RedirectToAction(nameof(Index));
        }

        private async Task FillWarehouses() =>
            ViewBag.Warehouses = new SelectList(await _db.Warehouses.ToListAsync(), "Id", "Name");
    }
}
