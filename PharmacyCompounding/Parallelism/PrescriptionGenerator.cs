using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using PharmacyCompounding.Data;

namespace PharmacyCompounding.Parallelism
{
    // Fills the prescriptions table with generated test prescriptions (for the parallel processing benchmark).
    // Generated prescriptions have numbers "GEN-000001", … and no order
    public static class PrescriptionGenerator
    {
        public const string Prefix = "GEN-";

        private static readonly string[] Surnames =
            { "Шевченко", "Коваленко", "Бондаренко", "Ткаченко", "Кравченко", "Олійник", "Мельник", "Поліщук", "Савченко", "Руденко", "Мороз", "Лисенко" };
        private static readonly string[] Initials = { "А. В.", "О. М.", "І. П.", "Н. С.", "Д. О.", "М. І.", "Т. В.", "С. А." };

        // Formula templates with the amount ranges of each component (Latin names in the genitive)
        private static readonly (string Name, double Min, double Max)[][] Formulas =
        {
            new (string, double, double)[] { ("Zinci oxydi", 2, 10), ("Vaselini", 20, 90) },
            new (string, double, double)[] { ("Acidi borici", 1, 5), ("Vaselini", 10, 50) },
            new (string, double, double)[] { ("Ichthyoli", 1, 5), ("Lanolini anhydrici", 5, 15), ("Vaselini", 10, 30) },
            new (string, double, double)[] { ("Natrii chloridi", 0.9, 9), ("Aquae purificatae", 100, 500) },
            new (string, double, double)[] { ("Glucosi", 5, 25), ("Aquae purificatae", 100, 250) },
            new (string, double, double)[] { ("Mentholi", 0.1, 1), ("Olei persicorum", 10, 30) },
            new (string, double, double)[] { ("Anaesthesini", 0.5, 3), ("Novocaini", 0.5, 2), ("Vaselini", 20, 50) },
        };

        private static readonly int[] Validity = { 10, 14, 30, 60, 90 };

        public static Task<int> CountAsync(PharmacyContext db) =>
            db.Prescriptions.CountAsync(p => p.Number.StartsWith(Prefix));

        // Inserts `count` prescriptions with PostgreSQL COPY (binary import) inside one transaction
        public static async Task<int> GenerateAsync(PharmacyContext db, int count)
        {
            var doctorIds = await db.Doctors.Select(d => d.Id).ToArrayAsync();
            if (doctorIds.Length == 0) throw new InvalidOperationException("Спочатку додайте хоча б одного лікаря");
            var start = await CountAsync(db);
            var random = new Random(2026 + start);   // a fixed seed makes the data reproducible
            var today = DateTime.Today;

            await using var transaction = await db.Database.BeginTransactionAsync();
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using (var writer = await connection.BeginBinaryImportAsync(
                "COPY prescriptions (number, issue_date, validity_days, patient, formula, doctor_id) FROM STDIN (FORMAT BINARY)"))
            {
                for (var i = 1; i <= count; i++)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync($"{Prefix}{start + i:D6}", NpgsqlDbType.Text);
                    await writer.WriteAsync(today.AddDays(-random.Next(0, 730)), NpgsqlDbType.Timestamp);
                    await writer.WriteAsync(Validity[random.Next(Validity.Length)], NpgsqlDbType.Integer);
                    await writer.WriteAsync($"{Surnames[random.Next(Surnames.Length)]} {Initials[random.Next(Initials.Length)]}", NpgsqlDbType.Text);
                    await writer.WriteAsync(Formula(random), NpgsqlDbType.Text);
                    await writer.WriteAsync(doctorIds[random.Next(doctorIds.Length)], NpgsqlDbType.Integer);
                }
                await writer.CompleteAsync();
            }
            await transaction.CommitAsync();
            return count;
        }

        // Generated prescriptions are not linked to orders, so they can be removed without touching real data
        public static Task<int> DeleteAsync(PharmacyContext db) =>
            db.Prescriptions.Where(p => p.Number.StartsWith(Prefix) && p.Order == null).ExecuteDeleteAsync();

        // "Rp.: Zinci oxydi 5,0; Vaselini ad 50,0. M.f. ung."
        private static string Formula(Random random)
        {
            var template = Formulas[random.Next(Formulas.Length)];
            var parts = template.Select((c, i) =>
            {
                var amount = Math.Round(c.Min + random.NextDouble() * (c.Max - c.Min), 1);
                var ad = i == template.Length - 1 ? "ad " : "";
                return $"{c.Name} {ad}{amount.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')}";
            });
            return "Rp.: " + string.Join("; ", parts) + ". M.f.";
        }
    }
}
