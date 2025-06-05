using System.Text.Json.Serialization;

namespace LogEmbarcado.API.Models
{
    public class PerformanceMetric
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EndpointPath { get; set; }
        public string HttpMethod { get; set; }
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
        public string? UserIdentifier { get; set; }
        public string? RequestId { get; set; }
        public string? AdditionalData { get; set; }

        [JsonPropertyName("cpuUsage")]
        public string CpuUsageFormatted => $"{Math.Round(CpuUsagePercent, 2):F2}%";

        [JsonPropertyName("memoryUsage")]
        public string MemoryUsageFormatted => FormatMemory(MemoryUsageBytes);

        [JsonPropertyName("responseTime")]
        public string ResponseTimeFormatted => FormatDuration(DurationMs);

        private static string FormatMemory(long bytes)
        {
            if (bytes >= 1_073_741_824) // >= 1GB
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576) // >= 1MB
                return $"{bytes / 1_048_576.0:F1} MB";
            return $"{bytes / 1024.0:F1} KB";
        }

        private static string FormatDuration(long ms)
        {
            return ms >= 1000 ? $"{ms / 1000.0:F2}s" : $"{ms} ms";
        }
    }
}
