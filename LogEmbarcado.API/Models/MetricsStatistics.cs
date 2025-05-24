namespace LogEmbarcado.API.Models
{
    public class MetricsStatistics
    {
        public int TotalPerformanceRecords { get; set; }
        public int TotalErrorRecords { get; set; }
        public int Last24HoursPerformance { get; set; }
        public int Last24HoursErrors { get; set; }
        public int Last7DaysPerformance { get; set; }
        public int Last7DaysErrors { get; set; }
        public int Last30DaysPerformance { get; set; }
        public int Last30DaysErrors { get; set; }
        public DateTime? OldestPerformanceRecord { get; set; }
        public DateTime? NewestPerformanceRecord { get; set; }
        public long DatabaseSizeBytes { get; set; }

        public double DatabaseSizeMB => DatabaseSizeBytes / (1024.0 * 1024.0);
    }
}
