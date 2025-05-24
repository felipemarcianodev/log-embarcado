using LogEmbarcado.API.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogEmbarcado.API.Controllers
{
    [ApiController]
    [Route("metrics")]
    [Authorize(Roles = "Admin")] // Restrinja o acesso conforme necessário
    public class MetricsController : ControllerBase
    {
        #region Private Fields

        private readonly MetricsDbContext _context;

        #endregion Private Fields

        #region Public Constructors

        public MetricsController(MetricsDbContext context)
        {
            _context = context;
        }

        #endregion Public Constructors

        #region Public Methods

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.ErrorMetrics.AsQueryable();

            if (from.HasValue)
                query = query.Where(e => e.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.Timestamp <= to.Value);

            var metrics = await query
                .OrderByDescending(e => e.Timestamp)
                .Take(1000)
                .ToListAsync();

            return Ok(metrics);
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformanceMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.PerformanceMetrics.AsQueryable();

            if (from.HasValue)
                query = query.Where(p => p.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.Timestamp <= to.Value);

            // Limitar resultados para evitar problemas de performance
            var metrics = await query
                .OrderByDescending(p => p.Timestamp)
                .Take(1000)
                .ToListAsync();

            return Ok(metrics);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var now = DateTime.UtcNow;
            var lastHour = now.AddHours(-1);
            var lastDay = now.AddDays(-1);

            var hourlyMetrics = await _context.PerformanceMetrics
                .Where(p => p.Timestamp >= lastHour)
                .ToListAsync();

            var dailyMetrics = await _context.PerformanceMetrics
                .Where(p => p.Timestamp >= lastDay)
                .ToListAsync();

            var hourlyErrors = await _context.ErrorMetrics
                .Where(e => e.Timestamp >= lastHour)
                .CountAsync();

            var dailyErrors = await _context.ErrorMetrics
                .Where(e => e.Timestamp >= lastDay)
                .CountAsync();

            return Ok(new
            {
                LastHour = new
                {
                    RequestCount = hourlyMetrics.Count,
                    AverageResponseTime = hourlyMetrics.Any() ? hourlyMetrics.Average(m => m.DurationMs) : 0,
                    ErrorCount = hourlyErrors,
                    AverageCpuUsage = hourlyMetrics.Any() ? hourlyMetrics.Average(m => m.CpuUsagePercent) : 0,
                    AverageMemoryUsage = hourlyMetrics.Any() ? hourlyMetrics.Average(m => m.MemoryUsageBytes) : 0
                },
                LastDay = new
                {
                    RequestCount = dailyMetrics.Count,
                    AverageResponseTime = dailyMetrics.Any() ? dailyMetrics.Average(m => m.DurationMs) : 0,
                    ErrorCount = dailyErrors,
                    AverageCpuUsage = dailyMetrics.Any() ? dailyMetrics.Average(m => m.CpuUsagePercent) : 0,
                    AverageMemoryUsage = dailyMetrics.Any() ? dailyMetrics.Average(m => m.MemoryUsageBytes) : 0
                }
            });
        }

        #endregion Public Methods
    }
}