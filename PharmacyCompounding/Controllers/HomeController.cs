using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    public class HomeController : Controller
    {
        private readonly PharmacyContext _db;

        public HomeController(PharmacyContext db) => _db = db;

        // GET: /  — dashboard; clients are redirected to their personal account
        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(Roles.Client)) return RedirectToAction("Index", "Cabinet");

            var orders = await _db.Orders.Include(o => o.Client).ToListAsync();
            ViewBag.ByStatus = orders.GroupBy(o => o.Status)
                                     .OrderBy(g => g.Key)
                                     .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.Recent = orders.OrderByDescending(o => o.CreatedAt).Take(5).ToList();
            ViewBag.LowStock = await _db.Ingredients.Where(i => i.Stock < i.MinStock).ToListAsync();
            ViewBag.Revenue = await _db.Payments.Where(p => p.Successful).SumAsync(p => (double)p.Amount);
            ViewBag.ClientsCount = await _db.Clients.CountAsync();
            ViewBag.IngredientsCount = await _db.Ingredients.CountAsync();
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
