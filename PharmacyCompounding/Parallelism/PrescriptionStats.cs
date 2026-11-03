using System.Globalization;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Parallelism
{
    // Statistics on prescriptions for one doctor
    public class DoctorStats
    {
        public int Prescriptions { get; set; }
        public int Valid { get; set; }
        public int Components { get; set; }
        public double Mass { get; set; }

        // Ingredient counts keyed by the hash of the name, so a name string is created only the first time it is seen
        private readonly Dictionary<int, (string Name, int Count)> _ingredients = new();

        public IEnumerable<(string Name, int Count)> Ingredients => _ingredients.Values;

        public void AddIngredient(ReadOnlySpan<char> name)
        {
            var key = string.GetHashCode(name);
            _ingredients[key] = _ingredients.TryGetValue(key, out var x) ? (x.Name, x.Count + 1) : (name.ToString(), 1);
        }

        public void Merge(DoctorStats other)
        {
            Prescriptions += other.Prescriptions;
            Valid += other.Valid;
            Components += other.Components;
            Mass += other.Mass;
            foreach (var (key, (name, count)) in other._ingredients)
                _ingredients[key] = _ingredients.TryGetValue(key, out var x) ? (x.Name, x.Count + count) : (name, count);
        }
    }

    // Result of the prescription analysis. Every thread fills its own instance, and the partial results
    // are merged at the end, so threads never write to shared data
    public class PrescriptionStats
    {
        private static readonly NumberFormatInfo Comma = new() { NumberDecimalSeparator = "," };

        public Dictionary<int, DoctorStats> ByDoctor { get; } = new();

        // Analyzes one prescription: parses the formula "Rp.: Zinci oxydi 5,0; Vaselini ad 50,0. M.f." into
        // components (Latin name and amount in grams or ml) and checks whether the prescription is still valid.
        // The formula is parsed with spans: no memory is allocated, and no object is shared between threads
        // (with one shared Regex object the threads waited for each other, and 18 threads were only ~1.6× faster)
        public void Add(Prescription p, DateTime onDate)
        {
            if (!ByDoctor.TryGetValue(p.DoctorId, out var stats))
                ByDoctor[p.DoctorId] = stats = new DoctorStats();
            stats.Prescriptions++;
            if (p.IsValid(onDate)) stats.Valid++;
            var formula = p.Formula.AsSpan();
            if (formula.StartsWith("Rp.: ")) formula = formula[5..];
            var end = formula.IndexOf(". ");
            if (end >= 0) formula = formula[..end];
            while (!formula.IsEmpty)
            {
                var sep = formula.IndexOf(';');
                var component = (sep < 0 ? formula : formula[..sep]).Trim();
                formula = sep < 0 ? ReadOnlySpan<char>.Empty : formula[(sep + 1)..];
                var space = component.LastIndexOf(' ');
                if (space <= 0 || !double.TryParse(component[(space + 1)..], NumberStyles.Float, Comma, out var amount)) continue;
                var name = component[..space];
                if (name.EndsWith(" ad")) name = name[..^3];
                stats.Components++;
                stats.Mass += amount;
                stats.AddIngredient(name);
            }
        }

        public PrescriptionStats Merge(PrescriptionStats other)
        {
            foreach (var (doctorId, stats) in other.ByDoctor)
            {
                if (ByDoctor.TryGetValue(doctorId, out var mine)) mine.Merge(stats);
                else ByDoctor[doctorId] = stats;
            }
            return this;
        }

        // A short fingerprint of the result, to check that every technology computed the same thing
        public string Checksum =>
            string.Join(";", ByDoctor.OrderBy(d => d.Key).Select(d =>
                $"{d.Key}:{d.Value.Prescriptions}:{d.Value.Valid}:{d.Value.Components}:{Math.Round(d.Value.Mass, 1)}"));
    }
}
