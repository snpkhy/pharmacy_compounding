using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Reports: LINQ queries against the PostgreSQL database (filtering, aggregation, data maintenance)
    public class ReportsController : Controller
    {
        private readonly PharmacyContext _db;

        public ReportsController(PharmacyContext db) => _db = db;

        // Statuses in which the medicine has been manufactured (ingredients written off)
        private static readonly OrderStatus[] Manufactured =
            { OrderStatus.InProduction, OrderStatus.QualityControl, OrderStatus.Ready, OrderStatus.Issued };

        // GET: /Reports — list of reports
        [HttpGet]
        public IActionResult Index() => View();

        // ---------- Filtering ----------

        // Query 1. Orders for a period, filtered by status and client
        [HttpGet]
        public async Task<IActionResult> Orders(DateTime? from, DateTime? to, OrderStatus? status, int? clientId)
        {
            var dateFrom = from ?? DateTime.Today.AddMonths(-6);
            var dateTo = to ?? DateTime.Today;
            var vm = Describe(nameof(Orders), "Вибірки", "Замовлення за період",
                "Замовлення, створені у вказаний період, з відбором за статусом і клієнтом.");
            vm.Param("from", "Дата з", "date", dateFrom)
              .Param("to", "Дата по", "date", dateTo)
              .Param("status", "Статус", "select", status, StatusOptions(status))
              .Param("clientId", "Клієнт", "select", clientId, await ClientOptions(clientId));

            var query = _db.Orders.Where(o => o.CreatedAt >= dateFrom && o.CreatedAt < dateTo.AddDays(1));
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);
            if (clientId.HasValue)
                query = query.Where(o => o.ClientId == clientId.Value);

            var orders = await query
                .OrderBy(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Number,
                    o.CreatedAt,
                    Client = o.Client!.FullName,
                    Doctor = o.Prescription!.Doctor!.FullName,
                    Medicine = o.Items.Select(i => i.MedicineName).FirstOrDefault(),
                    o.Status,
                    o.Total
                })
                .ToListAsync();

            vm.Table("№", "Створено", "Клієнт", "Лікар", "Ліки", "Статус", "Сума, ₴");
            foreach (var o in orders)
                vm.Row(o.Number, o.CreatedAt, o.Client, o.Doctor, o.Medicine, o.Status, o.Total);
            vm.Total("Знайдено замовлень", orders.Count);
            vm.Total("На суму, ₴", orders.Sum(o => o.Total));
            return Show(vm);
        }

        // Query 2. Doctors of a given specialty and the number of prescriptions they wrote (LINQ query syntax)
        [HttpGet]
        public async Task<IActionResult> Doctors(string? specialization, int minPrescriptions = 1)
        {
            var vm = Describe(nameof(Doctors), "Вибірки", "Лікарі та їхні рецепти",
                "Лікарі обраної спеціалізації, які виписали не менше вказаної кількості рецептів, і дата їхнього останнього рецепта.");
            var specs = await _db.Doctors.Where(d => d.Specialization != null)
                                         .Select(d => d.Specialization!).Distinct().OrderBy(s => s).ToListAsync();
            vm.Param("specialization", "Спеціалізація", "select", specialization,
                     specs.Select(s => new SelectListItem(s, s, s == specialization)).Prepend(new SelectListItem("— усі —", "")))
              .Param("minPrescriptions", "Не менше рецептів", "number", minPrescriptions);

            var doctors = await (from d in _db.Doctors
                                 where specialization == null || d.Specialization == specialization
                                 let count = d.Prescriptions.Count()
                                 where count >= minPrescriptions
                                 orderby count descending, d.FullName
                                 select new
                                 {
                                     d.FullName,
                                     d.Specialization,
                                     d.Institution,
                                     d.LicenseNumber,
                                     Count = count,
                                     LastIssued = d.Prescriptions.Max(p => (DateTime?)p.IssueDate)
                                 }).ToListAsync();

            vm.Table("Лікар", "Спеціалізація", "Заклад", "Ліцензія", "Рецептів", "Останній рецепт");
            foreach (var d in doctors)
                vm.Row(d.FullName, d.Specialization, d.Institution, d.LicenseNumber, d.Count, d.LastIssued);
            vm.Total("Лікарів", doctors.Count);
            vm.Total("Рецептів усього", doctors.Sum(d => d.Count));
            return Show(vm);
        }

        // Query 3. Ingredients that are low on stock or expired
        [HttpGet]
        public async Task<IActionResult> Shortages(int? warehouseId, string mode = "all", int days = 30)
        {
            var vm = Describe(nameof(Shortages), "Вибірки", "Проблемні залишки на складі",
                "Інгредієнти, яких менше за мінімальний залишок, а також прострочені або ті, термін яких спливає найближчим часом.");
            vm.Param("warehouseId", "Склад", "select", warehouseId, await WarehouseOptions(warehouseId, allowAll: true))
              .Param("mode", "Що шукати", "select", mode, new[]
              {
                  new SelectListItem("Усі проблеми", "all", mode == "all"),
                  new SelectListItem("Нижче мінімуму", "low", mode == "low"),
                  new SelectListItem("Термін спливає / минув", "expiry", mode == "expiry")
              })
              .Param("days", "Спливає протягом, днів", "number", days);

            var today = DateTime.Today;
            var limit = today.AddDays(days);
            var query = _db.Ingredients.AsQueryable();
            if (warehouseId.HasValue)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);
            query = mode switch
            {
                "low" => query.Where(i => i.Stock < i.MinStock),
                "expiry" => query.Where(i => i.ExpiryDate <= limit),
                _ => query.Where(i => i.Stock < i.MinStock || i.ExpiryDate <= limit)
            };

            var items = await query.OrderBy(i => i.ExpiryDate)
                .Select(i => new { i.Code, i.Name, Warehouse = i.Warehouse!.Name, i.Stock, i.MinStock, i.Unit, i.ExpiryDate })
                .ToListAsync();

            vm.Table("Код", "Інгредієнт", "Склад", "Залишок", "Мінімум", "Придатний до", "Проблема");
            foreach (var i in items)
            {
                var problems = new List<string>();
                if (i.Stock < i.MinStock) problems.Add($"бракує {i.MinStock - i.Stock:0.##} {i.Unit}");
                if (i.ExpiryDate < today) problems.Add("прострочений");
                else if (i.ExpiryDate <= limit) problems.Add($"спливає через {(i.ExpiryDate - today).Days} дн.");
                vm.Row(i.Code, i.Name, i.Warehouse, i.Stock, i.MinStock, i.ExpiryDate, string.Join(", ", problems));
            }
            vm.Total("Позицій", items.Count);
            return Show(vm);
        }

        // ---------- Aggregation ----------

        // Query 4. Revenue for a chosen period: day, month or half-year
        [HttpGet]
        public async Task<IActionResult> Revenue(string period = "month", DateTime? date = null)
        {
            var day = (date ?? DateTime.Today).Date;
            var (start, end, label) = period switch
            {
                "day" => (day, day.AddDays(1), $"за {day:dd.MM.yyyy}"),
                "halfyear" => day.Month <= 6
                    ? (new DateTime(day.Year, 1, 1), new DateTime(day.Year, 7, 1), $"за І півріччя {day.Year} р.")
                    : (new DateTime(day.Year, 7, 1), new DateTime(day.Year + 1, 1, 1), $"за ІІ півріччя {day.Year} р."),
                _ => (new DateTime(day.Year, day.Month, 1), new DateTime(day.Year, day.Month, 1).AddMonths(1), $"за {day:MMMM yyyy}")
            };

            var vm = Describe(nameof(Revenue), "Підсумки", "Виручка за період",
                "Виручка за день, місяць або півріччя, що містить вказану дату, з розбивкою за способом оплати.");
            vm.Param("period", "Період", "select", period, new[]
              {
                  new SelectListItem("День", "day", period == "day"),
                  new SelectListItem("Місяць", "month", period == "month"),
                  new SelectListItem("Півріччя", "halfyear", period == "halfyear")
              })
              .Param("date", "Дата в періоді", "date", day);

            var payments = _db.Payments.Where(p => p.Successful && p.Date >= start && p.Date < end);
            var byMethod = await payments
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Count = g.Count(), Sum = g.Sum(p => p.Amount), Avg = g.Average(p => p.Amount) })
                .OrderByDescending(x => x.Sum)
                .ToListAsync();
            var total = await payments.SumAsync(p => p.Amount);
            var count = await payments.CountAsync();

            vm.ResultTitle = $"Виручка {label} ({start:dd.MM.yyyy} – {end.AddDays(-1):dd.MM.yyyy})";
            vm.Table("Спосіб оплати", "Оплат", "Сума, ₴", "Середній чек, ₴");
            foreach (var m in byMethod)
                vm.Row(m.Method, m.Count, m.Sum, m.Avg);
            vm.Total("Виручка, ₴", total);
            vm.Total("Оплат", count);
            vm.Total("Середній чек, ₴", count > 0 ? total / count : 0m);
            return Show(vm);
        }

        // Query 5. Revenue by month (grouped by year and month)
        [HttpGet]
        public async Task<IActionResult> Monthly(int months = 6)
        {
            var vm = Describe(nameof(Monthly), "Підсумки", "Виручка по місяцях",
                "Кількість оплат, виручка, середній і найбільший чек за кожен місяць.");
            vm.Param("months", "Останні місяців", "number", months);

            var since = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
            var stats = await _db.Payments
                .Where(p => p.Successful && p.Date >= since)
                .GroupBy(p => new { p.Date.Year, p.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Sum = g.Sum(p => p.Amount),
                    Avg = g.Average(p => p.Amount),
                    Max = g.Max(p => p.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            vm.Table("Місяць", "Оплат", "Сума, ₴", "Середня, ₴", "Найбільша, ₴");
            foreach (var s in stats)
                vm.Row(new DateTime(s.Year, s.Month, 1).ToString("MMMM yyyy"), s.Count, s.Sum, s.Avg, s.Max);
            vm.Total("Разом, ₴", stats.Sum(s => s.Sum));
            vm.Total("Оплат", stats.Sum(s => s.Count));
            vm.Total("Найкращий місяць", stats.Count > 0 ? new DateTime(stats.MaxBy(s => s.Sum)!.Year, stats.MaxBy(s => s.Sum)!.Month, 1).ToString("MMMM yyyy") : null);
            return Show(vm);
        }

        // Query 6. Ingredient consumption for manufacturing over a period (join and grouping)
        [HttpGet]
        public async Task<IActionResult> Consumption(DateTime? from, DateTime? to)
        {
            var dateFrom = from ?? DateTime.Today.AddMonths(-6);
            var dateTo = to ?? DateTime.Today;
            var vm = Describe(nameof(Consumption), "Підсумки", "Витрата інгредієнтів",
                "Скільки кожного інгредієнта витрачено на виготовлені замовлення за період і на яку суму.");
            vm.Param("from", "Дата з", "date", dateFrom)
              .Param("to", "Дата по", "date", dateTo);

            var usage = await (from c in _db.Components
                               join item in _db.OrderItems on c.OrderItemId equals item.Id
                               join o in _db.Orders on item.OrderId equals o.Id
                               join i in _db.Ingredients on c.IngredientId equals i.Id
                               where Manufactured.Contains(o.Status)
                                     && o.CreatedAt >= dateFrom && o.CreatedAt < dateTo.AddDays(1)
                               group new { c.Amount, item.Quantity, o.Id } by new { i.Code, i.Name, i.Unit, i.PricePerUnit } into g
                               select new
                               {
                                   g.Key.Code,
                                   g.Key.Name,
                                   g.Key.Unit,
                                   g.Key.PricePerUnit,
                                   Amount = g.Sum(x => x.Amount * x.Quantity),
                                   Orders = g.Select(x => x.Id).Distinct().Count()
                               })
                              .OrderByDescending(x => x.Amount * (double)x.PricePerUnit)
                              .ToListAsync();

            vm.Table("Код", "Інгредієнт", "Витрачено", "Од.", "Замовлень", "Ціна за од., ₴", "Вартість, ₴");
            foreach (var u in usage)
                vm.Row(u.Code, u.Name, u.Amount, u.Unit, u.Orders, u.PricePerUnit, (decimal)u.Amount * u.PricePerUnit);
            vm.Total("Інгредієнтів", usage.Count);
            vm.Total("Вартість сировини, ₴", usage.Sum(u => (decimal)u.Amount * u.PricePerUnit));
            return Show(vm);
        }

        // Query 7. Pharm technologist performance (correlated subqueries)
        [HttpGet]
        public async Task<IActionResult> Technologists()
        {
            var vm = Describe(nameof(Technologists), "Підсумки", "Робота фармацевтів-технологів",
                "Для кожного технолога: кількість актів виготовлення, з них прийнятих і забракованих на контролі якості, частка браку.");

            var stats = await _db.Technologists
                .Select(t => new
                {
                    t.FullName,
                    t.PersonnelNumber,
                    Acts = t.ManufacturingActs.Count(),
                    Passed = t.ManufacturingActs.Count(a => a.QualityCheck != null && a.QualityCheck.Result),
                    Defects = t.ManufacturingActs.Count(a => a.QualityCheck != null && !a.QualityCheck.Result),
                    Last = t.ManufacturingActs.Max(a => (DateTime?)a.ManufacturedAt)
                })
                .OrderByDescending(x => x.Acts)
                .ToListAsync();

            vm.Table("Технолог", "Таб. №", "Актів", "Прийнято", "Брак", "Частка браку", "Останнє виготовлення");
            foreach (var s in stats)
                vm.Row(s.FullName, s.PersonnelNumber, s.Acts, s.Passed, s.Defects,
                       s.Acts > 0 ? $"{100.0 * s.Defects / s.Acts:0.#}%" : "—", s.Last);
            vm.Total("Актів усього", stats.Sum(s => s.Acts));
            vm.Total("Забраковано", stats.Sum(s => s.Defects));
            return Show(vm);
        }

        // Query 8. Cost of manufactured items, computed in parallel in memory (PLINQ)
        [HttpGet]
        public async Task<IActionResult> Cost()
        {
            var vm = Describe(nameof(Cost), "Підсумки", "Собівартість ліків",
                "Вартість сировини й роботи для кожного виду ліків і частка сировини в ціні.");

            var items = await _db.OrderItems.AsNoTracking()
                .Include(i => i.Components).ThenInclude(c => c.Ingredient)
                .Where(i => Manufactured.Contains(i.Order!.Status))
                .ToListAsync();

            var watch = Stopwatch.StartNew();
            var byMedicine = items.AsParallel()
                .Select(i => new
                {
                    i.MedicineName,
                    Materials = i.Components.Sum(c => c.Cost()) * i.Quantity,
                    Work = i.WorkCost * i.Quantity,
                    Total = i.CalculateCost()
                })
                .GroupBy(x => x.MedicineName)
                .Select(g => new
                {
                    Medicine = g.Key,
                    Count = g.Count(),
                    Materials = g.Sum(x => x.Materials),
                    Work = g.Sum(x => x.Work),
                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Total)
                .ToList();
            watch.Stop();

            vm.Table("Ліки", "Виготовлено", "Сировина, ₴", "Робота, ₴", "Разом, ₴", "Частка сировини");
            foreach (var m in byMedicine)
                vm.Row(m.Medicine, m.Count, m.Materials, m.Work, m.Total, m.Total > 0 ? $"{100 * m.Materials / m.Total:0.#}%" : "—");
            vm.Total("Позицій оброблено", items.Count);
            vm.Total("Ядер процесора", Environment.ProcessorCount);
            vm.Total("Час обчислення PLINQ, мс", watch.Elapsed.TotalMilliseconds);
            return Show(vm);
        }

        // ---------- Insert, update, delete ----------

        // Query 9. Add a new doctor
        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> AddDoctor()
        {
            var vm = DescribeAddDoctor(new Doctor());
            await RecentDoctors(vm);
            return Show(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> AddDoctor(Doctor doctor)
        {
            var vm = DescribeAddDoctor(doctor);
            if (string.IsNullOrWhiteSpace(doctor.FullName) || string.IsNullOrWhiteSpace(doctor.LicenseNumber))
                vm.Error = "Вкажіть ПІБ і номер ліцензії";
            else if (await _db.Doctors.AnyAsync(d => d.LicenseNumber == doctor.LicenseNumber))
                vm.Error = $"Лікар з ліцензією {doctor.LicenseNumber} уже є в базі";
            else
            {
                _db.Doctors.Add(doctor);
                await _db.SaveChangesAsync();
                vm.Message = $"Додано лікаря «{doctor.FullName}»";
            }
            await RecentDoctors(vm);
            return Show(vm);
        }

        // Query 10. Reprice a warehouse's ingredients by a given percentage
        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Reprice(int? warehouseId)
        {
            var vm = await DescribeReprice(warehouseId, 10);
            await WarehousePrices(vm, warehouseId);
            return Show(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Reprice(int warehouseId, decimal percent)
        {
            var vm = await DescribeReprice(warehouseId, percent);
            if (percent <= -100 || percent > 500)
                vm.Error = "Відсоток має бути в межах від −99 до 500";
            else
            {
                var factor = 1 + percent / 100;
                var updated = await _db.Ingredients
                    .Where(i => i.WarehouseId == warehouseId)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.PricePerUnit, i => Math.Round(i.PricePerUnit * factor, 2)));
                vm.Message = $"Ціни змінено на {percent:+0.##;−0.##}% для {updated} інгредієнтів";
            }
            await WarehousePrices(vm, warehouseId);
            return Show(vm);
        }

        // Query 11. Delete old cancelled orders together with their items and prescriptions
        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Cleanup(int days = 30)
        {
            var vm = DescribeCleanup(days);
            await CancelledOrders(vm);
            return Show(vm);
        }

        [HttpPost, ActionName(nameof(Cleanup))]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> CleanupConfirmed(int days)
        {
            var vm = DescribeCleanup(days);
            var border = DateTime.Today.AddDays(-days);
            var cancelled = _db.Orders.Where(o => o.Status == OrderStatus.Cancelled && o.CreatedAt < border);

            // ExecuteDelete runs its statement right away, outside SaveChanges, so the three statements are put
            // into one explicit transaction: either the orders and their prescriptions are all deleted, or nothing is
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var prescriptionIds = await cancelled.Select(o => o.PrescriptionId).ToListAsync();
            // The database deletes items, components and payments itself (ON DELETE CASCADE)
            var orders = await cancelled.ExecuteDeleteAsync();
            // Only the prescriptions of the deleted orders
            var prescriptions = await _db.Prescriptions
                .Where(p => prescriptionIds.Contains(p.Id))
                .ExecuteDeleteAsync();
            await transaction.CommitAsync();
            vm.Message = $"Видалено замовлень: {orders}, рецептів: {prescriptions}";
            await CancelledOrders(vm);
            return Show(vm);
        }

        // ---------- Helpers ----------

        private IActionResult Show(ReportViewModel vm)
        {
            vm.Sql = SqlCaptureInterceptor.For(HttpContext);
            return View("Report", vm);
        }

        private static ReportViewModel Describe(string action, string kind, string title, string description) =>
            new() { Action = action, Kind = kind, Title = title, Description = description };

        private static ReportViewModel DescribeAddDoctor(Doctor d)
        {
            var vm = Describe(nameof(AddDoctor), "Обслуговування", "Додати лікаря",
                "Внесення нового лікаря до довідника. Нижче — нещодавно додані лікарі.");
            vm.IsPost = true;
            vm.SubmitText = "Додати";
            return vm.Param(nameof(Doctor.FullName), "ПІБ", "text", d.FullName)
                     .Param(nameof(Doctor.Specialization), "Спеціалізація", "text", d.Specialization)
                     .Param(nameof(Doctor.Institution), "Заклад охорони здоров'я", "text", d.Institution)
                     .Param(nameof(Doctor.LicenseNumber), "Номер ліцензії", "text", d.LicenseNumber);
        }

        private async Task RecentDoctors(ReportViewModel vm)
        {
            var doctors = await _db.Doctors.OrderByDescending(d => d.Id).Take(5).ToListAsync();
            vm.ResultTitle = "Нещодавно додані лікарі";
            vm.Table("Лікар", "Спеціалізація", "Заклад", "Ліцензія");
            foreach (var d in doctors)
                vm.Row(d.FullName, d.Specialization, d.Institution, d.LicenseNumber);
            vm.Total("Лікарів у довіднику", await _db.Doctors.CountAsync());
        }

        private async Task<ReportViewModel> DescribeReprice(int? warehouseId, decimal percent)
        {
            var vm = Describe(nameof(Reprice), "Обслуговування", "Переоцінка інгредієнтів",
                "Зміна ціни всіх інгредієнтів обраного складу на вказаний відсоток. Від'ємний відсоток — знижка.");
            vm.IsPost = true;
            vm.SubmitText = "Змінити ціни";
            return vm.Param("warehouseId", "Склад", "select", warehouseId, await WarehouseOptions(warehouseId, allowAll: false))
                     .Param("percent", "Зміна, %", "number", percent);
        }

        private async Task WarehousePrices(ReportViewModel vm, int? warehouseId)
        {
            var id = warehouseId ?? await _db.Warehouses.OrderBy(w => w.Id).Select(w => w.Id).FirstAsync();
            var items = await _db.Ingredients.Where(i => i.WarehouseId == id).OrderBy(i => i.Code).ToListAsync();
            vm.ResultTitle = "Поточні ціни інгредієнтів складу";
            vm.Table("Код", "Інгредієнт", "Од.", "Ціна за од., ₴", "Залишок", "Вартість залишку, ₴");
            foreach (var i in items)
                vm.Row(i.Code, i.Name, i.Unit, i.PricePerUnit, i.Stock, i.PricePerUnit * (decimal)i.Stock);
            vm.Total("Вартість залишків складу, ₴", items.Sum(i => i.PricePerUnit * (decimal)i.Stock));
        }

        private static ReportViewModel DescribeCleanup(int days)
        {
            var vm = Describe(nameof(Cleanup), "Обслуговування", "Очищення скасованих замовлень",
                "Видалення давніх скасованих замовлень разом із їхніми позиціями та рецептами, які більше ніде не використовуються.");
            vm.IsPost = true;
            vm.SubmitText = "Видалити";
            return vm.Param("days", "Старші за, днів", "number", days);
        }

        private async Task CancelledOrders(ReportViewModel vm)
        {
            var orders = await _db.Orders.Where(o => o.Status == OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new { o.Number, o.CreatedAt, Client = o.Client!.FullName, Rx = o.Prescription!.Number, o.Total })
                .ToListAsync();
            vm.ResultTitle = "Скасовані замовлення";
            vm.Table("№", "Створено", "Клієнт", "Рецепт", "Сума, ₴", "Днів тому");
            foreach (var o in orders)
                vm.Row(o.Number, o.CreatedAt, o.Client, o.Rx, o.Total, (DateTime.Today - o.CreatedAt.Date).Days);
            vm.Total("Скасованих замовлень", orders.Count);
            vm.Total("Рецептів усього", await _db.Prescriptions.CountAsync());
        }

        private static IEnumerable<SelectListItem> StatusOptions(OrderStatus? selected) =>
            Enum.GetValues<OrderStatus>()
                .Select(s => new SelectListItem(s.GetDisplayName(), s.ToString(), s == selected))
                .Prepend(new SelectListItem("— усі —", ""));

        private async Task<IEnumerable<SelectListItem>> ClientOptions(int? selected) =>
            (await _db.Clients.OrderBy(c => c.FullName).ToListAsync())
                .Select(c => new SelectListItem(c.FullName, c.Id.ToString(), c.Id == selected))
                .Prepend(new SelectListItem("— усі —", ""));

        private async Task<IEnumerable<SelectListItem>> WarehouseOptions(int? selected, bool allowAll)
        {
            var list = (await _db.Warehouses.OrderBy(w => w.Id).ToListAsync())
                .Select(w => new SelectListItem(w.Name, w.Id.ToString(), w.Id == selected));
            return allowAll ? list.Prepend(new SelectListItem("— усі —", "")) : list;
        }
    }
}
