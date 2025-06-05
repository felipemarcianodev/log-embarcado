using LogEmbarcado.API;
using LogEmbarcado.API.Context;
using LogEmbarcado.API.Extensions;
using LogEmbarcado.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddControllers();

builder.Services.AddSingleton<MetricsService>();
builder.Services.AddHostedService<MetricsCleanupService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LogEmbarcado API",
        Version = "v1",
        Description = "API para monitoramento de logs e métricas de performance",
        Contact = new OpenApiContact
        {
            Name = "Seu Nome",
            Email = "seu.email@exemplo.com"
        }
    });
    // Incluir comentários XML se você quiser documentação detalhada
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

if (builder.Configuration.GetValue<bool>("FeatureFlags:DetailedPerformanceLogging", true))
{
    var dbPath = Folders.GetDatabasePath();

    builder.Services.AddDbContext<MetricsDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
}
#endregion


#region Configure Pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogEmbarcado API v1");
        c.RoutePrefix = string.Empty;
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        //c.DefaultModelsExpandDepth(-1);
    });
}

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
