using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Controllers
{
    // Pharmacy contacts: open to clients and staff
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly PharmacyContext _db;

        public ContactsController(PharmacyContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index() =>
            View(await _db.Users.OfType<Employee>().OrderBy(e => e.Position).ThenBy(e => e.FullName).ToListAsync());
    }
}
