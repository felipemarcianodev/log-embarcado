using LogEmbarcado.API.Middlewares;

namespace LogEmbarcado.API.Extensions
{
    public static class PerformanceMonitoringMiddlewareExtension
    {
        #region Public Methods

        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }

        #endregion Public Methods
    }
}
