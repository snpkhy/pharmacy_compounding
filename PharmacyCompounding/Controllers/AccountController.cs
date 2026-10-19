using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Sign-in, sign-up and sign-out. The session is kept in an encrypted cookie
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly PharmacyContext _db;

        public AccountController(PharmacyContext db) => _db = db;

        [HttpGet]
        public IActionResult Login(string? returnUrl) =>
            User.Identity?.IsAuthenticated == true
                ? RedirectToAction("Index", "Home")
                : View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email!.ToLower() == email);
            if (user == null || !user.Login(vm.Email.Trim(), vm.Password))
            {
                ModelState.AddModelError("", "Невірний email або пароль");
                return View(vm);
            }

            await SignIn(user, vm.RememberMe);
            return Url.IsLocalUrl(vm.ReturnUrl) ? Redirect(vm.ReturnUrl) : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() =>
            User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new RegisterViewModel());

        // Only clients can sign up themselves; employees are added by the management
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            var email = vm.Email.Trim();
            if (await _db.Users.AnyAsync(u => u.Email!.ToLower() == email.ToLower()))
                ModelState.AddModelError(nameof(vm.Email), "Користувач з таким email уже зареєстрований");
            if (!ModelState.IsValid) return View(vm);

            var client = new Client { FullName = vm.FullName.Trim(), Email = email, Phone = vm.Phone };
            client.SetPassword(vm.Password);
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();

            await SignIn(client, false);
            TempData["Message"] = $"Вітаємо, {client.FullName}! Обліковий запис створено";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private Task SignIn(User user, bool persistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Role, user.Role)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = persistent });
        }
    }
}
