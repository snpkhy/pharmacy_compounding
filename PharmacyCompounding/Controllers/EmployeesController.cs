using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Any employee can view the staff, but only the manager can add, edit and dismiss employees
    public class EmployeesController : Controller
    {
        private readonly PharmacyContext _db;

        public EmployeesController(PharmacyContext db) => _db = db;

        // Polymorphic query: selects only Employee subclasses from the Users table
        [HttpGet]
        public async Task<IActionResult> Index() =>
            View(await _db.Users.OfType<Employee>().OrderBy(e => e.Position).ThenBy(e => e.FullName).ToListAsync());

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public IActionResult Create() => View(new EmployeeFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Create(EmployeeFormViewModel vm)
        {
            if (string.IsNullOrEmpty(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Вкажіть початковий пароль");
            await Validate(vm);
            if (!ModelState.IsValid) return View(vm);

            var employee = vm.CreateEmployee();
            vm.ApplyTo(employee);
            _db.Add(employee);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Працівника «{employee.FullName}» додано. Передайте йому email і пароль для входу";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _db.Users.OfType<Employee>().FirstOrDefaultAsync(e => e.Id == id);
            return employee == null ? NotFound() : View(EmployeeFormViewModel.From(employee));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Edit(int id, EmployeeFormViewModel vm)
        {
            var employee = await _db.Users.OfType<Employee>().FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();
            vm.Id = id;
            vm.Type = employee.Role;   // the role (class) cannot change after creation
            await Validate(vm);
            if (!ModelState.IsValid) return View(vm);

            vm.ApplyTo(employee);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Дані працівника оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Users.OfType<Employee>().FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();

            var hasHistory = employee switch
            {
                Pharmacist p => await _db.QualityChecks.AnyAsync(q => q.PharmacistId == p.Id),
                PharmTechnologist t => await _db.ManufacturingActs.AnyAsync(a => a.TechnologistId == t.Id),
                _ => false
            };
            if (employee.Id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier))
                TempData["Error"] = "Неможливо видалити власний обліковий запис";
            else if (hasHistory)
                TempData["Error"] = "Неможливо видалити працівника, який уже має акти виготовлення або контролю якості";
            else
            {
                _db.Remove(employee);
                await _db.SaveChangesAsync();
                TempData["Message"] = $"Працівника «{employee.FullName}» видалено";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task Validate(EmployeeFormViewModel vm)
        {
            if (!EmployeeFormViewModel.Types.ContainsKey(vm.Type))
                ModelState.AddModelError(nameof(vm.Type), "Оберіть роль");
            if (!string.IsNullOrWhiteSpace(vm.Email) &&
                await _db.Users.AnyAsync(u => u.Id != vm.Id && u.Email!.ToLower() == vm.Email.Trim().ToLower()))
                ModelState.AddModelError(nameof(vm.Email), "Користувач з таким email уже зареєстрований");
        }
    }
}
