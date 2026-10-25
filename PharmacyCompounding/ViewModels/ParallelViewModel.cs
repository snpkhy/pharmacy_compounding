using PharmacyCompounding.Parallelism;

namespace PharmacyCompounding.ViewModels
{
    // Parallel processing page: data size, read times, benchmark rows and the analysis result
    public class ParallelViewModel
    {
        public string? Dataset { get; set; }
        public int Total { get; set; }
        public int Generated { get; set; }
        public int Cores { get; set; }
        public int[] ThreadCounts { get; set; } = Array.Empty<int>();

        public int Records { get; set; }
        public double SequentialReadMs { get; set; }
        public double? ParallelReadMs { get; set; }
        public bool ParallelReadSame { get; set; }

        public List<BenchmarkRow> Rows { get; set; } = new();
        public List<(string Doctor, DoctorStats Stats)> Doctors { get; set; } = new();
    }
}
