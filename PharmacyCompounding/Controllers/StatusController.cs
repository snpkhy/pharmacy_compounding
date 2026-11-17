using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    // "View the order status": the use case diagram (lab 1) lets a client check it by the order number
    // without signing in, so only the status and the ready date are shown, no personal data
    [AllowAnonymous]
    public class StatusController : Controller
    {
        private readonly PharmacyContext _db;

        public StatusController(PharmacyContext db) => _db = db;

        // GET: /Status?number=1002
        [HttpGet]
        public async Task<IActionResult> Index(int? number)
        {
            ViewBag.Number = number;
            if (number == null) return View();
            // Only the public fields are read from the database
            var order = await _db.Orders.AsNoTracking()
                .Where(o => o.Number == number)
                .Select(o => new Order { Number = o.Number, Status = o.Status, PlannedReadyDate = o.PlannedReadyDate })
                .FirstOrDefaultAsync();
            return View(order);
        }
    }
}
