using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;
using PharmacyCompounding.Parallelism;
using PharmacyCompounding.ViewModels;

namespace PharmacyCompounding.Controllers
{
    // Parallel data processing: the same query run sequentially and in parallel with different technologies
    public class ParallelController : Controller
    {
        private const int GenerateCount = 100_000;
        private const int SmallSize = 20;
        // Number of database connections used to read the data in parallel
        private const int ReadConnections = 4;

        private readonly PharmacyContext _db;
        private readonly DbContextOptions<PharmacyContext> _options;

        public ParallelController(PharmacyContext db, DbContextOptions<PharmacyContext> options)
        {
            _db = db;
            _options = options;
        }

        // GET: /Parallel?dataset=small|all
        [HttpGet]
        public async Task<IActionResult> Index(string? dataset)
        {
            var vm = new ParallelViewModel
            {
                Dataset = dataset,
                Total = await _db.Prescriptions.CountAsync(),
                Generated = await PrescriptionGenerator.CountAsync(_db),
                Cores = Environment.ProcessorCount,
                ThreadCounts = new[] { 2, 4, 8, Environment.ProcessorCount }.Distinct().ToArray()
            };
            if (dataset is not ("small" or "all")) return View(vm);

            // 1. The selection query: sequential read vs. parallel read over several connections
            var watch = Stopwatch.StartNew();
            var data = dataset == "small" ? await LoadSmall() : await Load(_db, null);
            vm.SequentialReadMs = watch.Elapsed.TotalMilliseconds;
            if (dataset == "all")
            {
                watch.Restart();
                var parallel = await LoadParallel();
                vm.ParallelReadMs = watch.Elapsed.TotalMilliseconds;
                vm.ParallelReadSame = parallel.Count == data.Count;
            }

            // 2. Processing the selected rows in memory with every technology
            var (rows, result) = ParallelBenchmark.Run(data, DateTime.Today, vm.ThreadCounts);
            vm.Rows = rows;
            vm.Records = data.Count;
            var doctors = await _db.Doctors.ToDictionaryAsync(d => d.Id);
            vm.Doctors = result.ByDoctor
                .Select(d => (Doctor: doctors.GetValueOrDefault(d.Key)?.FullName ?? $"#{d.Key}", Stats: d.Value))
                .OrderByDescending(d => d.Stats.Prescriptions)
                .ToList();
            return View(vm);
        }

        // POST: /Parallel/Generate — adds 100 000 generated prescriptions
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Generate()
        {
            var watch = Stopwatch.StartNew();
            var added = await PrescriptionGenerator.GenerateAsync(_db, GenerateCount);
            TempData["Message"] = $"Додано {added:N0} рецептів за {watch.Elapsed.TotalSeconds:0.0} с";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> Clear()
        {
            var deleted = await PrescriptionGenerator.DeleteAsync(_db);
            TempData["Message"] = $"Видалено {deleted:N0} згенерованих рецептів";
            return RedirectToAction(nameof(Index));
        }

        // Real prescriptions only (at most 20 records)
        private Task<List<Prescription>> LoadSmall() =>
            Project(_db.Prescriptions.Where(p => !p.Number.StartsWith(PrescriptionGenerator.Prefix)))
                .OrderBy(p => p.Id).Take(SmallSize).ToListAsync();

        // All prescriptions, or the rows with ids in [from, to)
        private static Task<List<Prescription>> Load(PharmacyContext db, (int From, int To)? range)
        {
            var query = db.Prescriptions.AsQueryable();
            if (range is var (from, to)) query = query.Where(p => p.Id >= from && p.Id < to);
            return Project(query).ToListAsync();
        }

        // The id range is split between several contexts; each has its own connection, so the queries run at the same time
        private async Task<List<Prescription>> LoadParallel()
        {
            var bounds = await _db.Prescriptions.GroupBy(p => 1)
                .Select(g => new { Min = g.Min(p => p.Id), Max = g.Max(p => p.Id) }).SingleAsync();
            var size = (bounds.Max - bounds.Min) / ReadConnections + 1;
            var parts = await Task.WhenAll(Enumerable.Range(0, ReadConnections).Select(async i =>
            {
                await using var db = new PharmacyContext(_options);
                var from = bounds.Min + i * size;
                return await Load(db, (from, from + size));
            }));
            return parts.SelectMany(p => p).ToList();
        }

        // Only the columns the analysis needs, without change tracking
        private static IQueryable<Prescription> Project(IQueryable<Prescription> query) =>
            query.AsNoTracking().Select(p => new Prescription
            {
                Id = p.Id, DoctorId = p.DoctorId, IssueDate = p.IssueDate, ValidityDays = p.ValidityDays, Formula = p.Formula
            });
    }
}
