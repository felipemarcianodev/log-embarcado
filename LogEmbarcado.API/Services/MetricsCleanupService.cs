using LogEmbarcado.API.Context;
using Microsoft.EntityFrameworkCore;

namespace LogEmbarcado.API.Services
{
    public class MetricsCleanupService : BackgroundService
    {
        #region Private Fields

        private readonly TimeSpan _cleanupInterval;
        private readonly int _daysToKeepData;
        private readonly ILogger<MetricsCleanupService> _logger;
        private readonly long _maxDatabaseSizeBytes;
        private readonly IServiceProvider _serviceProvider;

        #endregion Private Fields

        #region Public Constructors

        public MetricsCleanupService(
            IServiceProvider serviceProvider,
            ILogger<MetricsCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cleanupInterval = TimeSpan.FromHours(6); // Executar a cada 6 horas
            _daysToKeepData = 30; // Manter 30 dias de dados
            _maxDatabaseSizeBytes = 100 * 1024 * 1024; // 100MB limite
        }

        #endregion Public Constructors

        #region Public Methods

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando serviço de limpeza de métricas...");

            // Executar uma limpeza final antes de parar
            try
            {
                await PerformCleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante limpeza final");
            }

            await base.StopAsync(cancellationToken);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de limpeza de métricas iniciado. Executando a cada {Interval} horas",
                _cleanupInterval.TotalHours);

            // Executar limpeza inicial após 1 minuto (para não impactar o startup)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante execução da limpeza de métricas");
                }

                // Aguardar próxima execução
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Serviço sendo parado
                    break;
                }
            }

            _logger.LogInformation("Serviço de limpeza de métricas finalizado");
        }

        #endregion Protected Methods

        #region Private Methods

        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                var backupFolder = Folders.GetBackupFolder();

                if (!Directory.Exists(backupFolder))
                    return;

                var backupFiles = Directory.GetFiles(backupFolder, "metrics_backup_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Manter apenas os 10 backups mais recentes
                var filesToDelete = backupFiles.Skip(10);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file.FullName);
                        _logger.LogDebug("Backup antigo removido: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao remover backup antigo: {FileName}", file.Name);
                    }
                }

                if (filesToDelete.Any())
                {
                    _logger.LogInformation("Removidos {Count} backups antigos. Mantidos {Kept} backups mais recentes",
                        filesToDelete.Count(), Math.Min(10, backupFiles.Count));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar backups antigos");
            }
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                var dbPath = Folders.GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    _logger.LogWarning("Banco de dados de métricas não encontrado em: {Path}", dbPath);
                    return;
                }

                var backupFolder = Folders.GetBackupFolder();

                // Criar pasta de backup se não existir
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                    _logger.LogInformation("Pasta de backup criada: {Path}", backupFolder);
                }

                var backupFileName = $"metrics_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var backupPath = Path.Combine(backupFolder, backupFileName);

                // Copiar o arquivo do banco
                File.Copy(dbPath, backupPath);

                _logger.LogInformation("Backup do banco de métricas criado: {BackupPath}", backupPath);

                // Verificar se o backup foi criado com sucesso
                var backupInfo = new FileInfo(backupPath);
                if (backupInfo.Length > 0)
                {
                    _logger.LogInformation("Backup criado com sucesso. Tamanho: {Size:N0} bytes", backupInfo.Length);
                }
                else
                {
                    _logger.LogWarning("Backup criado mas arquivo está vazio");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar backup do banco de métricas");
            }
        }

        private async Task EnforceStorageLimitsAsync()
        {
            try
            {
                var dbPath = Folders.GetDatabasePath();

                if (!File.Exists(dbPath))
                    return;

                var fileInfo = new FileInfo(dbPath);

                if (fileInfo.Length <= _maxDatabaseSizeBytes)
                {
                    _logger.LogDebug("Banco de métricas dentro do limite de tamanho: {Size:N0} bytes", fileInfo.Length);
                    return;
                }

                _logger.LogWarning("Banco de métricas excedeu o limite de {LimitMB:N0}MB. Tamanho atual: {CurrentMB:N0}MB. Removendo registros antigos...",
                    _maxDatabaseSizeBytes / (1024 * 1024),
                    fileInfo.Length / (1024 * 1024));

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                // Estatísticas antes da limpeza
                var performanceCount = await dbContext.PerformanceMetrics.CountAsync();
                var errorCount = await dbContext.ErrorMetrics.CountAsync();

                _logger.LogInformation("Registros antes da limpeza - Performance: {PerformanceCount:N0}, Erros: {ErrorCount:N0}",
                    performanceCount, errorCount);

                // Remover 40% dos registros mais antigos
                await RemoveOldestRecords(dbContext, performanceCount, errorCount, 0.4);

                await dbContext.SaveChangesAsync();

                // Estatísticas após a limpeza
                var newPerformanceCount = await dbContext.PerformanceMetrics.CountAsync();
                var newErrorCount = await dbContext.ErrorMetrics.CountAsync();

                _logger.LogInformation("Registros após a limpeza - Performance: {NewPerformanceCount:N0}, Erros: {NewErrorCount:N0}",
                    newPerformanceCount, newErrorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aplicar limites de armazenamento");
            }
        }

        private async Task PerformCleanupAsync()
        {
            _logger.LogInformation("Iniciando limpeza de métricas...");

            using var scope = _serviceProvider.CreateScope();
            var metricsService = scope.ServiceProvider.GetRequiredService<MetricsService>();

            // 1. Fazer backup antes da limpeza
            await CreateBackupAsync();

            // 2. Verificar tamanho do banco e aplicar limpeza se necessário
            await EnforceStorageLimitsAsync();

            // 3. Limpar dados antigos baseado na data
            await metricsService.CleanupOldMetricsAsync(_daysToKeepData);

            // 4. Limpar backups antigos
            await CleanupOldBackupsAsync();

            // 5. Vacuum do SQLite para recuperar espaço
            await VacuumDatabaseAsync();

            _logger.LogInformation("Limpeza de métricas concluída");
        }

        private async Task RemoveOldestRecords(MetricsDbContext dbContext, int performanceCount, int errorCount, double percentageToRemove)
        {
            // Remover registros de performance
            if (performanceCount > 1000)
            {
                var deleteCount = (int)(performanceCount * percentageToRemove);
                var oldestPerformanceRecords = await dbContext.PerformanceMetrics
                    .OrderBy(p => p.Timestamp)
                    .Take(deleteCount)
                    .ToListAsync();

                if (oldestPerformanceRecords.Any())
                {
                    dbContext.PerformanceMetrics.RemoveRange(oldestPerformanceRecords);
                    _logger.LogInformation("Removendo {Count:N0} registros de performance mais antigos", oldestPerformanceRecords.Count);
                }
            }

            // Remover registros de erro
            if (errorCount > 1000)
            {
                var deleteCount = (int)(errorCount * percentageToRemove);
                var oldestErrorRecords = await dbContext.ErrorMetrics
                    .OrderBy(e => e.Timestamp)
                    .Take(deleteCount)
                    .ToListAsync();

                if (oldestErrorRecords.Any())
                {
                    dbContext.ErrorMetrics.RemoveRange(oldestErrorRecords);
                    _logger.LogInformation("Removendo {Count:N0} registros de erro mais antigos", oldestErrorRecords.Count);
                }
            }
        }

        private async Task VacuumDatabaseAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                // Obter tamanho antes do vacuum
                var dbPath = Folders.GetDatabasePath();
                if (!File.Exists(dbPath))
                    return;

                var sizeBefore = new FileInfo(dbPath).Length;

                // Executar VACUUM no SQLite para recuperar espaço
                await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");

                // Obter tamanho depois do vacuum
                var sizeAfter = new FileInfo(dbPath).Length;
                var spaceMB = (sizeBefore - sizeAfter) / (1024.0 * 1024.0);

                if (spaceMB > 0.1) // Log apenas se recuperou mais de 0.1MB
                {
                    _logger.LogInformation("VACUUM executado. Espaço recuperado: {SpaceRecovered:N1}MB", spaceMB);
                }
                else
                {
                    _logger.LogDebug("VACUUM executado. Pouco ou nenhum espaço recuperado");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar VACUUM no banco de dados");
            }
        }

        #endregion Private Methods
    }
}