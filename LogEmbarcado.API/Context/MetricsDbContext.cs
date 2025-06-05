using LogEmbarcado.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LogEmbarcado.API.Context
{
    public class MetricsDbContext : DbContext
    {
        #region Public Constructors

        public MetricsDbContext(DbContextOptions<MetricsDbContext> options)
            : base(options)
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public DbSet<ErrorMetric> ErrorMetrics { get; set; }

        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade PerformanceMetric
            modelBuilder.Entity<PerformanceMetric>(entity =>
            {
                // Chave primária
                entity.HasKey(p => p.Id);

                // Configurações de propriedades
                entity.Property(p => p.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.Timestamp)
                    .IsRequired()
                    .HasColumnType("datetime");

                entity.Property(p => p.EndpointPath)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("varchar(500)");

                entity.Property(p => p.HttpMethod)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnType("varchar(10)");

                entity.Property(p => p.StatusCode)
                    .IsRequired()
                    .HasColumnType("integer");

                entity.Property(p => p.DurationMs)
                    .IsRequired()
                    .HasColumnType("bigint");

                entity.Property(p => p.CpuUsagePercent)
                    .IsRequired()
                    .HasColumnType("real")
                    .HasPrecision(5, 2);

                entity.Property(p => p.MemoryUsageBytes)
                    .IsRequired()
                    .HasColumnType("bigint");

                entity.Property(p => p.RequestId)
                    .HasMaxLength(100)
                    .HasColumnType("varchar(100)");

                entity.Property(p => p.UserIdentifier)
                    .HasMaxLength(100)
                    .HasColumnType("varchar(100)");

                entity.Property(p => p.AdditionalData)
                    .HasMaxLength(500)
                    .HasColumnType("varchar(500)");

                // Índices para performance
                entity.HasIndex(p => p.Timestamp)
                    .HasDatabaseName("IX_PerformanceMetrics_Timestamp");

                entity.HasIndex(p => p.EndpointPath)
                    .HasDatabaseName("IX_PerformanceMetrics_EndpointPath");

                entity.HasIndex(p => p.StatusCode)
                    .HasDatabaseName("IX_PerformanceMetrics_StatusCode");

                entity.HasIndex(p => p.RequestId)
                    .HasDatabaseName("IX_PerformanceMetrics_RequestId");

                // Índice composto para consultas por período e endpoint
                entity.HasIndex(p => new { p.Timestamp, p.EndpointPath })
                    .HasDatabaseName("IX_PerformanceMetrics_Timestamp_EndpointPath");

                // Índice composto para consultas por data e status
                entity.HasIndex(p => new { p.Timestamp, p.StatusCode })
                    .HasDatabaseName("IX_PerformanceMetrics_Timestamp_StatusCode");

                // Nome da tabela
                entity.ToTable("PerformanceMetrics");
            });

            // Configuração da entidade ErrorMetric
            modelBuilder.Entity<ErrorMetric>(entity =>
            {
                // Chave primária
                entity.HasKey(e => e.Id);

                // Configurações de propriedades
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnType("datetime");

                entity.Property(e => e.EndpointPath)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("varchar(500)");

                entity.Property(e => e.ExceptionType)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("varchar(200)");

                entity.Property(e => e.ExceptionMessage)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasColumnType("varchar(1000)");

                entity.Property(e => e.StackTrace)
                    .HasColumnType("text"); // Pode ser longo

                entity.Property(e => e.RequestId)
                    .HasMaxLength(100)
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.UserIdentifier)
                    .HasMaxLength(100)
                    .HasColumnType("varchar(100)");

                // Índices para performance
                entity.HasIndex(e => e.Timestamp)
                    .HasDatabaseName("IX_ErrorMetrics_Timestamp");

                entity.HasIndex(e => e.EndpointPath)
                    .HasDatabaseName("IX_ErrorMetrics_EndpointPath");

                entity.HasIndex(e => e.ExceptionType)
                    .HasDatabaseName("IX_ErrorMetrics_ExceptionType");

                entity.HasIndex(e => e.RequestId)
                    .HasDatabaseName("IX_ErrorMetrics_RequestId");

                // Índice composto para consultas por período e endpoint
                entity.HasIndex(e => new { e.Timestamp, e.EndpointPath })
                    .HasDatabaseName("IX_ErrorMetrics_Timestamp_EndpointPath");

                // Índice composto para consultas por data e tipo de exceção
                entity.HasIndex(e => new { e.Timestamp, e.ExceptionType })
                    .HasDatabaseName("IX_ErrorMetrics_Timestamp_ExceptionType");

                // Nome da tabela
                entity.ToTable("ErrorMetrics");
            });

            // Configurações globais para SQLite
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // Configurar para usar UTC nas datas
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var property in entityType.GetProperties())
                    {
                        if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                        {
                            property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                        }
                    }
                }
            }
        }

        #endregion Protected Methods
    }
}