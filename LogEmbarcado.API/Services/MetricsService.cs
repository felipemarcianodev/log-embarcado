using LogEmbarcado.API.Context;
using LogEmbarcado.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LogEmbarcado.API.Services
{
    public class MetricsService
    {
        #region Private Fields

        private readonly Process _currentProcess;
        private readonly ILogger<MetricsService> _logger;
        private readonly IServiceProvider _serviceProvider;

        #endregion Private Fields

        #region Public Constructors

        public MetricsService(
            IServiceProvider serviceProvider, 
            ILogger<MetricsService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _currentProcess = Process.GetCurrentProcess();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task CleanupOldMetricsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

                _logger.LogInformation("Iniciando limpeza de métricas anteriores a {CutoffDate:yyyy-MM-dd}", cutoffDate);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                // Contar registros antes da limpeza
                var oldPerformanceCount = await dbContext.PerformanceMetrics
                    .Where(p => p.Timestamp < cutoffDate)
                    .CountAsync();

                var oldErrorCount = await dbContext.ErrorMetrics
                    .Where(e => e.Timestamp < cutoffDate)
                    .CountAsync();

                if (oldPerformanceCount == 0 && oldErrorCount == 0)
                {
                    _logger.LogDebug("Não há registros antigos para remover");
                    return;
                }

                _logger.LogInformation("Removendo {PerformanceCount:N0} registros de performance e {ErrorCount:N0} registros de erro anteriores a {Days} dias",
                    oldPerformanceCount, oldErrorCount, daysToKeep);

                // Remover métricas de performance antigas
                if (oldPerformanceCount > 0)
                {
                    // Remover em lotes para evitar problemas de memória
                    const int batchSize = 1000;
                    int removedCount = 0;

                    while (true)
                    {
                        var batch = await dbContext.PerformanceMetrics
                            .Where(p => p.Timestamp < cutoffDate)
                            .Take(batchSize)
                            .ToListAsync();

                        if (!batch.Any())
                            break;

                        dbContext.PerformanceMetrics.RemoveRange(batch);
                        await dbContext.SaveChangesAsync();

                        removedCount += batch.Count;
                        _logger.LogDebug("Removidos {BatchCount} registros de performance (total: {TotalRemoved}/{TotalToRemove})",
                            batch.Count, removedCount, oldPerformanceCount);

                        // Pequena pausa para não sobrecarregar o sistema
                        await Task.Delay(10);
                    }

                    _logger.LogInformation("Removidos {TotalCount:N0} registros de performance", removedCount);
                }

                // Remover métricas de erro antigas
                if (oldErrorCount > 0)
                {
                    const int batchSize = 1000;
                    int removedCount = 0;

                    while (true)
                    {
                        var batch = await dbContext.ErrorMetrics
                            .Where(e => e.Timestamp < cutoffDate)
                            .Take(batchSize)
                            .ToListAsync();

                        if (!batch.Any())
                            break;

                        dbContext.ErrorMetrics.RemoveRange(batch);
                        await dbContext.SaveChangesAsync();

                        removedCount += batch.Count;
                        _logger.LogDebug("Removidos {BatchCount} registros de erro (total: {TotalRemoved}/{TotalToRemove})",
                            batch.Count, removedCount, oldErrorCount);

                        await Task.Delay(10);
                    }

                    _logger.LogInformation("Removidos {TotalCount:N0} registros de erro", removedCount);
                }

                _logger.LogInformation("Limpeza de registros antigos concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar métricas antigas");
                throw;
            }
        }

        public async Task EnforceStorageLimitsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                // Verificar tamanho do arquivo SQLite
                var dbPath = Folders.GetDatabasePath();
                var fileInfo = new FileInfo(dbPath);

                // Se ultrapassar 100MB, remover os registros mais antigos
                const long MAX_SIZE_BYTES = 100 * 1024 * 1024; // 100MB

                if (fileInfo.Exists && fileInfo.Length > MAX_SIZE_BYTES)
                {
                    _logger.LogWarning("Banco de métricas excedeu o tamanho limite de 100MB. Removendo registros antigos.");

                    // Manter apenas os registros mais recentes
                    var performanceCount = await dbContext.PerformanceMetrics.CountAsync();
                    var errorCount = await dbContext.ErrorMetrics.CountAsync();

                    // Remover 30% dos registros mais antigos se o limite for ultrapassado
                    if (performanceCount > 1000)
                    {
                        var deleteCount = performanceCount / 3;
                        var oldestRecordsToDelete = await dbContext.PerformanceMetrics
                            .OrderBy(p => p.Timestamp)
                            .Take(deleteCount)
                            .ToListAsync();

                        dbContext.PerformanceMetrics.RemoveRange(oldestRecordsToDelete);
                    }

                    if (errorCount > 1000)
                    {
                        var deleteCount = errorCount / 3;
                        var oldestRecordsToDelete = await dbContext.ErrorMetrics
                            .OrderBy(e => e.Timestamp)
                            .Take(deleteCount)
                            .ToListAsync();

                        dbContext.ErrorMetrics.RemoveRange(oldestRecordsToDelete);
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar limites de armazenamento");
            }
        }

        public async Task<MetricsStatistics> GetDatabaseStatisticsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                var now = DateTime.UtcNow;
                var last24Hours = now.AddDays(-1);
                var last7Days = now.AddDays(-7);
                var last30Days = now.AddDays(-30);

                var stats = new MetricsStatistics
                {
                    TotalPerformanceRecords = await dbContext.PerformanceMetrics.CountAsync(),
                    TotalErrorRecords = await dbContext.ErrorMetrics.CountAsync(),

                    Last24HoursPerformance = await dbContext.PerformanceMetrics
                        .Where(p => p.Timestamp >= last24Hours)
                        .CountAsync(),

                    Last24HoursErrors = await dbContext.ErrorMetrics
                        .Where(e => e.Timestamp >= last24Hours)
                        .CountAsync(),

                    Last7DaysPerformance = await dbContext.PerformanceMetrics
                        .Where(p => p.Timestamp >= last7Days)
                        .CountAsync(),

                    Last7DaysErrors = await dbContext.ErrorMetrics
                        .Where(e => e.Timestamp >= last7Days)
                        .CountAsync(),

                    Last30DaysPerformance = await dbContext.PerformanceMetrics
                        .Where(p => p.Timestamp >= last30Days)
                        .CountAsync(),

                    Last30DaysErrors = await dbContext.ErrorMetrics
                        .Where(e => e.Timestamp >= last30Days)
                        .CountAsync(),

                    OldestPerformanceRecord = await dbContext.PerformanceMetrics
                        .OrderBy(p => p.Timestamp)
                        .Select(p => p.Timestamp)
                        .FirstOrDefaultAsync(),

                    NewestPerformanceRecord = await dbContext.PerformanceMetrics
                        .OrderByDescending(p => p.Timestamp)
                        .Select(p => p.Timestamp)
                        .FirstOrDefaultAsync(),

                    DatabaseSizeBytes = GetDatabaseFileSize()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do banco de dados");
                throw;
            }
        }

        public async Task RecordErrorMetricAsync(HttpContext context, Exception exception)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                var metric = new ErrorMetric
                {
                    Timestamp = DateTime.UtcNow,
                    EndpointPath = context.Request.Path,
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    RequestId = context.TraceIdentifier,
                    UserIdentifier = context.User?.Identity?.Name
                };

                dbContext.ErrorMetrics.Add(metric);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar métrica de erro");
            }
        }

        public async Task RecordPerformanceMetricAsync(HttpContext context, long durationMs)
        {
            try
            {
                // Capturar CPU e memória atual
                _currentProcess.Refresh();
                var cpuUsage = GetCpuUsage();
                var memoryUsage = _currentProcess.WorkingSet64;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                var metric = new PerformanceMetric
                {
                    Timestamp = DateTime.UtcNow,
                    EndpointPath = context.Request.Path,
                    HttpMethod = context.Request.Method,
                    StatusCode = context.Response.StatusCode,
                    DurationMs = durationMs,
                    CpuUsagePercent = cpuUsage,
                    MemoryUsageBytes = memoryUsage,
                    RequestId = context.TraceIdentifier,
                    UserIdentifier = context.User?.Identity?.Name
                };

                dbContext.PerformanceMetrics.Add(metric);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar métrica de performance");
            }
        }

        #endregion Public Methods

        #region Private Methods

        private double GetCpuUsage()
        {
            try
            {
                // Solução simplificada: usar o tempo total do processo
                _currentProcess.Refresh();

                // Calcular uma aproximação baseada no tempo total de CPU
                var totalCpuTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
                var upTime = (DateTime.UtcNow - _currentProcess.StartTime).TotalMilliseconds;

                // Porcentagem aproximada considerando todos os cores
                var cpuPercent = (totalCpuTime / upTime / Environment.ProcessorCount) * 100;

                var result = Math.Min(100, Math.Max(0, cpuPercent));

                // Arredondar para 4 casas decimais
                return Math.Round(result, 4);
            }
            catch
            {
                return 0.0;
            }
        }

        private long GetDatabaseFileSize()
        {
            try
            {
                var dbPath = Folders.GetDatabasePath();
                return File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion Private Methods
    }
}