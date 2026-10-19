using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

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
    }
}
