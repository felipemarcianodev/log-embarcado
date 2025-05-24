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
    }
}
