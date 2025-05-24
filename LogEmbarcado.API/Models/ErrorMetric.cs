namespace LogEmbarcado.API.Models
{
    public class ErrorMetric
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EndpointPath { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public string? RequestId { get; set; }
        public string? UserIdentifier { get; set; }
    }
}
