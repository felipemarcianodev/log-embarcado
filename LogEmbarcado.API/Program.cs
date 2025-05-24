using LogEmbarcado.API.Context;
using LogEmbarcado.API.Extensions;
using LogEmbarcado.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddControllers();

// Adicionar como serviço Singleton para uso no middleware
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddHostedService<MetricsCleanupService>();

if (builder.Configuration.GetValue<bool>("FeatureFlags:DetailedPerformanceLogging", true))
{
    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Metrics.db");

    builder.Services.AddDbContext<MetricsDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
}
#endregion


#region Configure Pipeline.
var app = builder.Build();

app.UsePerformanceMonitoring();

app.UseAuthorization();

app.MapControllers();

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var dbContext = serviceScope.ServiceProvider.GetService<MetricsDbContext>();
    
    // Cria o banco se não existir
    dbContext.Database.EnsureCreated();
}

app.Run();
#endregion
