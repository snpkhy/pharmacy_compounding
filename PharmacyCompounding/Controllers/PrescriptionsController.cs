using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;

namespace PharmacyCompounding.Controllers
{
    public class PrescriptionsController : Controller
    {
        private readonly PharmacyContext _db;

        public PrescriptionsController(PharmacyContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index() =>
            View(await _db.Prescriptions.Include(p => p.Doctor).Include(p => p.Order)
                          .OrderByDescending(p => p.IssueDate).ToListAsync());
    }
}
