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
            modelBuilder.Entity<PerformanceMetric>()
                .HasIndex(p => p.Timestamp);

            modelBuilder.Entity<PerformanceMetric>()
                .HasIndex(p => p.EndpointPath);

            modelBuilder.Entity<ErrorMetric>()
                .HasIndex(e => e.Timestamp);
        }

        #endregion Protected Methods
    }
}