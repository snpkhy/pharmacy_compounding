using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;

namespace PharmacyCompounding.Controllers
{
    public class ContactsController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index() => View();
    }
}
