using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;

namespace PharmacyCompounding.Controllers
{
    public class PrescriptionsController : Controller
    {
        private const int PageSize = 50;

        private readonly PharmacyContext _db;

        public PrescriptionsController(PharmacyContext db) => _db = db;

        // GET: /Prescriptions?search=...&page=2 — the table can hold ~100 000 rows, so it is shown page by page
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Prescriptions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => EF.Functions.ILike(p.Number, $"%{search}%") || EF.Functions.ILike(p.Patient, $"%{search}%"));

            var total = await query.CountAsync();
            var pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Clamp(page, 1, pages);
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.Pages = pages;
            ViewBag.Total = total;
            return View(await query.Include(p => p.Doctor).Include(p => p.Order)
                                   .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                                   .Skip((page - 1) * PageSize).Take(PageSize)
                                   .ToListAsync());
        }
    }
}
