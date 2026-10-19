# Architecture

ASP.NET Core MVC (.NET 8) + Entity Framework Core (Code First) + PostgreSQL (in Docker, together with pgAdmin).
The UI is server-rendered Razor pages styled with Bootstrap 5. The UI text is in Ukrainian.

## How a request flows

```
Browser ──GET /Orders/Details/5──►  Program.cs (routing, authentication, role check)
                                        │
                                        ▼
                          Controllers/OrdersController.Details(id = 5)
                                        │  reads and changes data through PharmacyContext (EF Core)
                                        ▼
                          Views/Orders/Details.cshtml  ◄── Order model
                                        │  rendered inside Views/Shared/_Layout.cshtml (sidebar)
                                        ▼
                                    HTML page
```

The URL `/{Controller}/{Action}/{id?}` maps to the `{Controller}Controller` class and its `{Action}` method.
The method returns `View(...)`, and MVC looks for the file `Views/{Controller}/{Action}.cshtml`.
For example, `/Orders` → `OrdersController.Index()` → `Views/Orders/Index.cshtml`.

## Where things live

```
pharmacy_compounding/
├── README.md               how to run the project
├── .env.example            .env template: PostgreSQL database name, user and password
├── docker-compose.yml      PostgreSQL + pgAdmin (with a pre-configured database connection)
├── pharmacy_compounding.sln
├── docs/ARCHITECTURE.md    this file
└── PharmacyCompounding/
    ├── Program.cs          entry point: service registration (database, authentication, roles),
    │                       migrations and test data on startup, default route
    ├── appsettings.json    database host and port (localhost:5432); credentials come from .env
    │
    ├── Models/             M: domain classes (from the lab 1 class diagram)
    │   ├── Users.cs        User → Client / Employee → Pharmacist, PharmTechnologist, PharmacyManager;
    │   │                   roles (Roles), password hashing, User.Login()
    │   ├── Order.cs        Order and the allowed status transitions (state diagram)
    │   ├── OrderStatus.cs  order statuses
    │   ├── OrderItem.cs    order item and its components
    │   ├── Prescription.cs, Doctor.cs, Ingredient.cs, Warehouse.cs,
    │   ├── Payment.cs, Production.cs (manufacturing act + quality check)
    │   └── EnumExtensions.cs   GetDisplayName(): the Ukrainian name of an enum value
    │
    ├── Data/
    │   ├── PharmacyContext.cs  DbContext: tables (DbSet) and the relationships between them
    │   ├── DbInitializer.cs    test data + the default manager
    │   └── SqlCaptureInterceptor.cs  records the executed SQL for the Queries page
    ├── Migrations/         generated EF Core migrations (do not edit by hand)
    │
    ├── ViewModels/         data for specific forms that doesn't map one-to-one to a model
    │   ├── OrderCreateViewModel.cs   New order form
    │   ├── AccountViewModels.cs      sign-in and sign-up forms
    │   ├── EmployeeFormViewModel.cs  employee form (one for all roles)
    │   └── QueryViewModel.cs         parameters, result table and SQL of one query
    │
    ├── Controllers/        C: handle requests
    │   ├── HomeController         dashboard; redirects clients to their account
    │   ├── AccountController      sign-in, sign-up, sign-out, access denied
    │   ├── CabinetController      client account: only their own orders
    │   ├── OrdersController       orders and their whole lifecycle
    │   ├── PrescriptionsController, IngredientsController (warehouse),
    │   ├── ClientsController, DoctorsController
    │   ├── EmployeesController    staff list; only the manager can add and edit employees
    │   └── QueriesController      lab 3: LINQ queries (filtering, aggregation, PLINQ, INSERT/UPDATE/DELETE)
    │
    ├── Views/              V: Razor templates (.cshtml = HTML + C#)
    │   ├── _ViewImports.cshtml    shared usings and tag helpers for all views
    │   ├── _ViewStart.cshtml      default layout (_Layout)
    │   ├── Shared/
    │   │   ├── _Layout.cshtml     page frame: SIDEBAR, messages, CSS/JS and icons
    │   │   ├── _AuthLayout.cshtml frame for the sign-in and sign-up pages (no sidebar)
    │   │   ├── _StatusBadge, _Initials   small reusable partials
    │   │   └── Error.cshtml
    │   └── {Controller}/{Action}.cshtml  pages; _Form.cshtml is the shared form for Create/Edit
    │
    └── wwwroot/            static files, served as is
        ├── css/site.css    OUR styles: colors, sidebar, panels, tables, statuses
        ├── favicon.svg     ℞ icon in the browser tab (+ favicon.ico and apple-touch-icon.png for older browsers and iOS)
        └── lib/            third-party libraries (do not edit): bootstrap, bootstrap-icons,
                            jquery, jquery-validation (+ unobtrusive)
```

## Access and roles

Configured in `Program.cs`:

- **By default** every page is available to staff only (`FallbackPolicy`: Pharmacist, Technologist, Manager).
- `[AllowAnonymous]`: available without signing in (`AccountController`, the error page).
- `[Authorize(Roles = Roles.Client)]`: clients only (`CabinetController`).
- `[Authorize(Roles = Roles.Manager)]`: the manager only (Create/Edit/Delete in `EmployeesController`).

The attribute can go on a whole controller or on a single action. Roles are stored in the cookie at sign-in (`AccountController.SignIn`).

## CSS: which framework is used

- **Bootstrap 5.1** (`wwwroot/lib/bootstrap`): a CSS framework with a grid (`row`, `col-md-6`), spacing (`mt-3`, `mb-2`, `gap-3`),
  flex utilities (`d-flex`, `align-items-center`), forms (`form-control`, `form-select`, `form-label`) and tables (`table`, `table-hover`).
  Docs: https://getbootstrap.com/docs/5.1/
- **Bootstrap Icons 1.11** (`wwwroot/lib/bootstrap-icons`): icons, e.g. `<i class="bi bi-receipt"></i>`.
  Catalog with names: https://icons.getbootstrap.com/
- **site.css**: our own styles on top of Bootstrap. It defines the colors (the `--brand`, `--muted` etc. variables in `:root`)
  and our classes: `sidebar`, `panel` / `panel-head` / `panel-body` (cards), `page-head` (page header),
  `btn-brand` / `btn-soft` / `btn-ghost-danger` (buttons), `pill st-ok` / `st-bad` / ... (status badges),
  `stat` (dashboard tiles), `flash` (messages), `auth-*` (sign-in pages).
- **jQuery + jquery-validation-unobtrusive**: only for client-side form validation based on the `[Required]`, `[EmailAddress]`
  etc. attributes in the models. Enabled on a page with `<partial name="_ValidationScriptsPartial" />`.

There is no frontend build (npm, webpack): the files are included with `<link>` and `<script>` tags in `_Layout.cshtml`.

## How to add a sidebar button (example: Reports)

It takes three steps: **controller → view → sidebar link**.

### 1. Controller: `Controllers/ReportsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;

namespace PharmacyCompounding.Controllers
{
    public class ReportsController : Controller
    {
        private readonly PharmacyContext _db;

        public ReportsController(PharmacyContext db) => _db = db;   // the database is injected automatically

        // GET: /Reports
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var revenue = await _db.Payments.Where(p => p.Successful).SumAsync(p => (double)p.Amount);
            ViewBag.Revenue = revenue;
            return View(await _db.Ingredients.OrderBy(i => i.Name).ToListAsync());   // → Views/Reports/Index.cshtml
        }
    }
}
```

The class name must end with `Controller`; the part before it (`Reports`) becomes the URL `/Reports`.

### 2. View: `Views/Reports/Index.cshtml`

Folder name = controller name, file name = action name.

```cshtml
@model IEnumerable<Ingredient>
@{ ViewData["Title"] = "Звіти"; }
<div class="page-head">
    <div><h1>Звіти</h1><div class="sub">Виручка: @(((double)ViewBag.Revenue).ToString("N2")) ₴</div></div>
</div>
<div class="panel">
    <table class="table table-hover align-middle">
        <thead><tr><th>Інгредієнт</th><th class="text-end">Залишок</th></tr></thead>
        <tbody>
        @foreach (var i in Model)
        {
            <tr><td class="cell-title">@i.Name</td><td class="text-end num">@i.Stock @i.Unit</td></tr>
        }
        </tbody>
    </table>
</div>
```

`_Layout.cshtml` with the sidebar is applied automatically (through `_ViewStart.cshtml`), so you only write the page content.
The `page-head`, `panel` and `table` classes make it look like the other pages.

### 3. Link: `Views/Shared/_Layout.cshtml`

In the `<nav class="side-nav">` block, in the `else` branch (staff menu), add a line:

```cshtml
<a class="@Active("Reports")" asp-controller="Reports" asp-action="Index"><i class="bi bi-bar-chart"></i>Звіти</a>
```

- `asp-controller` / `asp-action`: where the button leads (Razor builds `href="/Reports"`).
- `@Active("Reports")`: highlights the item while this page is open.
- `bi bi-bar-chart`: the icon; pick any from https://icons.getbootstrap.com/
- `<div class="nav-section">Name</div>`: a group heading, if you need a new group.
- A button for clients goes in the `@if (isClient)` branch.

### Extras, depending on the task

| Need | What to do |
|---|---|
| Manager only | `[Authorize(Roles = Roles.Manager)]` on the controller + wrap the link in `@if (isManager) { ... }` in `_Layout` |
| Available to clients | `[Authorize(Roles = Roles.Client)]` on the controller, link in the `isClient` branch |
| A form that saves something | a pair of actions `[HttpGet] Create()` (empty form) and `[HttpPost] [ValidateAntiForgeryToken] Create(Model m)` (save + `RedirectToAction`); in the view `<form asp-action="Create" method="post">`. Working example: `DoctorsController` + `Views/Doctors` |
| A new database table | class in `Models/` → `DbSet<...>` in `PharmacyContext` → `dotnet ef migrations add Name --project PharmacyCompounding` from the repository root → the migration is applied on startup |
| A message after an action | `TempData["Message"] = "..."` (green) or `TempData["Error"] = "..."` (red); `_Layout` shows it on the next page |
| Custom styles | add a class to `wwwroot/css/site.css`, using the color variables from `:root` if needed |

After changes, run `dotnet run` in the `PharmacyCompounding` folder (or restart it if it's already running).
