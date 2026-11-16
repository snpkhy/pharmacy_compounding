using System.Collections.Concurrent;
using System.Diagnostics;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.Parallelism
{
    // The same prescription analysis performed with different technologies (Strategy pattern).
    // Each subclass decides only how the work is split between threads
    public abstract class PrescriptionAnalyzer
    {
        protected PrescriptionAnalyzer(int threads) => Threads = threads;

        public abstract string Technology { get; }

        public int Threads { get; }

        public abstract PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate);

        // Splits the list into `parts` nearly equal ranges [from, to)
        protected static IEnumerable<(int From, int To)> Ranges(int count, int parts)
        {
            var size = (count + parts - 1) / parts;
            for (var from = 0; from < count; from += size)
                yield return (from, Math.Min(from + size, count));
        }

        protected static PrescriptionStats AnalyzeRange(IReadOnlyList<Prescription> data, int from, int to, DateTime onDate)
        {
            var stats = new PrescriptionStats();
            for (var i = from; i < to; i++) stats.Add(data[i], onDate);
            return stats;
        }
    }

    // Plain LINQ-style loop on one thread: the baseline
    public class SequentialAnalyzer : PrescriptionAnalyzer
    {
        public SequentialAnalyzer() : base(1) { }

        public override string Technology => "Послідовно (LINQ / foreach)";

        public override PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate) =>
            AnalyzeRange(data, 0, data.Count, onDate);
    }

    // PLINQ: AsParallel with a chosen number of cores; Aggregate gives every thread its own partial result
    public class PlinqAnalyzer : PrescriptionAnalyzer
    {
        public PlinqAnalyzer(int threads) : base(threads) { }

        public override string Technology => "PLINQ (AsParallel)";

        public override PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate) =>
            data.AsParallel()
                .WithDegreeOfParallelism(Threads)
                .Aggregate(
                    () => new PrescriptionStats(),
                    (stats, p) => { stats.Add(p, onDate); return stats; },
                    (a, b) => a.Merge(b),
                    stats => stats);
    }

    // Multithreading: System.Threading.Thread objects created and joined by hand
    public class ThreadAnalyzer : PrescriptionAnalyzer
    {
        public ThreadAnalyzer(int threads) : base(threads) { }

        public override string Technology => "Багатопоточність (Thread)";

        public override PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate)
        {
            var ranges = Ranges(data.Count, Threads).ToArray();
            var partial = new PrescriptionStats[ranges.Length];
            var threads = ranges.Select((r, i) => new Thread(() => partial[i] = AnalyzeRange(data, r.From, r.To, onDate))).ToArray();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
            return partial.Aggregate(new PrescriptionStats(), (all, p) => all.Merge(p));
        }
    }

    // TPL: Parallel.ForEach over ranges with thread-local results
    public class ParallelForEachAnalyzer : PrescriptionAnalyzer
    {
        public ParallelForEachAnalyzer(int threads) : base(threads) { }

        public override string Technology => "TPL (Parallel.ForEach)";

        public override PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate)
        {
            var result = new PrescriptionStats();
            var chunk = Math.Max(1, data.Count / (Threads * 4));
            Parallel.ForEach(
                Partitioner.Create(0, data.Count, chunk),
                new ParallelOptions { MaxDegreeOfParallelism = Threads },
                () => new PrescriptionStats(),
                (range, _, local) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++) local.Add(data[i], onDate);
                    return local;
                },
                local => { lock (result) result.Merge(local); });
            return result;
        }
    }

    // TPL: one Task per part of the data, waiting for all of them
    public class TaskAnalyzer : PrescriptionAnalyzer
    {
        public TaskAnalyzer(int threads) : base(threads) { }

        public override string Technology => "TPL (Task.Run + WhenAll)";

        public override PrescriptionStats Analyze(IReadOnlyList<Prescription> data, DateTime onDate)
        {
            var tasks = Ranges(data.Count, Threads)
                .Select(r => Task.Run(() => AnalyzeRange(data, r.From, r.To, onDate)))
                .ToArray();
            var partial = Task.WhenAll(tasks).GetAwaiter().GetResult();
            return partial.Aggregate(new PrescriptionStats(), (all, p) => all.Merge(p));
        }
    }

    public record BenchmarkRow(string Technology, int Threads, double Milliseconds, double Speedup, bool SameResult);

    // Runs every analyzer on the same data and measures the median time
    public static class ParallelBenchmark
    {
        public static IEnumerable<PrescriptionAnalyzer> Analyzers(IEnumerable<int> threadCounts)
        {
            yield return new SequentialAnalyzer();
            foreach (var n in threadCounts) yield return new PlinqAnalyzer(n);
            foreach (var n in threadCounts) yield return new ThreadAnalyzer(n);
            foreach (var n in threadCounts) yield return new ParallelForEachAnalyzer(n);
            foreach (var n in threadCounts) yield return new TaskAnalyzer(n);
        }

        public static (List<BenchmarkRow> Rows, PrescriptionStats Result) Run(
            IReadOnlyList<Prescription> data, DateTime onDate, IEnumerable<int> threadCounts)
        {
            // Small data is measured more times, because a single run takes microseconds
            var repeats = data.Count < 1000 ? 51 : 11;
            var analyzers = Analyzers(threadCounts).ToList();
            // Warm-up: every analyzer runs once before measuring (JIT compilation, thread pool, CPU frequency),
            // otherwise the first one measured, the sequential baseline, would look slower than it is
            foreach (var analyzer in analyzers) analyzer.Analyze(data, onDate);

            var rows = new List<BenchmarkRow>();
            PrescriptionStats? reference = null;
            double baseline = 0;
            foreach (var analyzer in analyzers)
            {
                var result = analyzer.Analyze(data, onDate);
                var times = new double[repeats];
                for (var i = 0; i < repeats; i++)
                {
                    var watch = Stopwatch.StartNew();
                    result = analyzer.Analyze(data, onDate);
                    times[i] = watch.Elapsed.TotalMilliseconds;
                }
                Array.Sort(times);
                var median = times[repeats / 2];
                reference ??= result;
                if (baseline == 0) baseline = median;
                rows.Add(new BenchmarkRow(analyzer.Technology, analyzer.Threads, median, baseline / median,
                                          result.Checksum == reference.Checksum));
            }
            return (rows, reference!);
        }
    }
}
