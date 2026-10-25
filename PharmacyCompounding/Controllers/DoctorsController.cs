using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly PharmacyContext _db;

        public DoctorsController(PharmacyContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Prescriptions are counted in the database: there can be tens of thousands of them
            ViewBag.Counts = await _db.Prescriptions.GroupBy(p => p.DoctorId)
                                      .Select(g => new { g.Key, Count = g.Count() })
                                      .ToDictionaryAsync(x => x.Key, x => x.Count);
            return View(await _db.Doctors.OrderBy(d => d.FullName).ToListAsync());
        }

        [HttpGet]
        [Authorize(Roles = Roles.Pharmacy)]
        public IActionResult Create() => View(new Doctor());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Create(Doctor doctor)
        {
            await ValidateLicense(doctor);
            if (!ModelState.IsValid) return View(doctor);
            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Лікаря «{doctor.FullName}» додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _db.Doctors.FindAsync(id);
            return doctor == null ? NotFound() : View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Pharmacy)]
        public async Task<IActionResult> Edit(int id, Doctor doctor)
        {
            if (id != doctor.Id) return BadRequest();
            await ValidateLicense(doctor);
            if (!ModelState.IsValid) return View(doctor);
            _db.Update(doctor);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Дані лікаря оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _db.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            // Prescriptions refer to the doctor with ON DELETE RESTRICT
            if (await _db.Prescriptions.AnyAsync(p => p.DoctorId == id))
                TempData["Error"] = $"Неможливо видалити лікаря «{doctor.FullName}»: у базі є виписані ним рецепти";
            else
            {
                _db.Doctors.Remove(doctor);
                await _db.SaveChangesAsync();
                TempData["Message"] = $"Лікаря «{doctor.FullName}» видалено";
            }
            return RedirectToAction(nameof(Index));
        }

        // The license number is unique (a unique index in the database as well)
        private async Task ValidateLicense(Doctor doctor)
        {
            if (await _db.Doctors.AnyAsync(d => d.Id != doctor.Id && d.LicenseNumber == doctor.LicenseNumber))
                ModelState.AddModelError(nameof(Doctor.LicenseNumber), "Лікар з таким номером ліцензії вже є в довіднику");
        }
    }
}
