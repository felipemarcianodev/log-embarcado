
using LogEmbarcado.API.Services;
using System.Diagnostics;

namespace LogEmbarcado.API.Middlewares
{
    public class PerformanceMonitoringMiddleware
    {
        #region Private Fields

        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private readonly MetricsService _metricsService;
        private readonly RequestDelegate _next;

        #endregion Private Fields

        #region Public Constructors

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            MetricsService metricsService,
            ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _metricsService = metricsService;
            _logger = logger;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Registrar a exceção
                await _metricsService.RecordErrorMetricAsync(context, ex);
                _logger.LogError(ex, "Exceção não tratada em requisição {Path}", context.Request.Path);
                throw;
            }
            finally
            {
                sw.Stop();
                // Registrar métricas apenas para endpoints de API (opcional)
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await _metricsService.RecordPerformanceMetricAsync(context, sw.ElapsedMilliseconds);
                }
            }
        }

        #endregion Public Methods
    }
}