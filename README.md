## Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.


# 📊 LogEmbarcado.API - Sistema de Monitoramento Self-Contained

Sistema completo de **log embarcado** para APIs ASP.NET Core com coleta automática de métricas de performance, registro de erros e sistema de backup integrado.

## 🚀 Características Principais

- ✅ **Sistema embarcado** - SQLite local, sem dependências externas
- ✅ **Coleta automática** - Middleware captura performance e erros
- ✅ **Background services** - Limpeza e backup automático
- ✅ **APIs RESTful** - Consulta de métricas via endpoints
- ✅ **Swagger integrado** - Documentação e testes interativos
- ✅ **Zero configuração** - Plug-and-play em qualquer projeto
- ✅ **Self-contained** - Deploy simples, sem infraestrutura externa

## 📋 Pré-requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- IDE de sua preferência (Visual Studio, VS Code, Rider)

## 🔧 Instalação

### 1. Clone o repositório
```bash
git clone https://github.com/seu-usuario/LogEmbarcado.API.git
cd LogEmbarcado.API
```

### 2. Instale os pacotes NuGet necessários
```bash
# Entity Framework Core para SQLite
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# Ferramentas do Entity Framework
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Swagger para documentação (já incluído no .NET 9)
dotnet add package Swashbuckle.AspNetCore
```

### 3. Restore e Build
```bash
dotnet restore
dotnet build
```

### 4. Execute a aplicação
```bash
dotnet run
```

A aplicação estará disponível em:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `https://localhost:5001` (página inicial)

## 📁 Estrutura do Projeto

```
LogEmbarcado.API/
├── Context/
│   └── MetricsDbContext.cs          # Contexto do Entity Framework
├── Controllers/
│   ├── MetricsController.cs         # API para consulta de métricas
│   └── TestController.cs            # Endpoints para testes
├── Dtos/
│   └── TestPerformanceDto.cs        # DTOs para validação
├── Extensions/
│   └── PerformanceMonitoringMiddlewareExtension.cs
├── Middlewares/
│   └── PerformanceMonitoringMiddleware.cs  # Captura automática
├── Models/
│   ├── ErrorMetric.cs               # Modelo para erros
│   ├── PerformanceMetric.cs         # Modelo para performance
│   └── MetricsStatistics.cs         # Estatísticas consolidadas
├── Services/
│   ├── MetricsService.cs            # Serviço principal
│   └── MetricsCleanupService.cs     # Background service
├── Folders.cs                       # Centralização de paths
└── Program.cs                       # Configuração da aplicação
```

## ⚙️ Configuração

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "FeatureFlags": {
    "DetailedPerformanceLogging": true
  }
}
```

### Configurações Disponíveis

| Configuração | Padrão | Descrição |
|-------------|---------|-----------|
| `DetailedPerformanceLogging` | `true` | Ativa/desativa o sistema de métricas |
| Intervalo de limpeza | 6 horas | Frequência do background service |
| Retenção de dados | 30 dias | Tempo para manter métricas antigas |
| Limite do banco | 100MB | Tamanho máximo do arquivo SQLite |
| Backups mantidos | 10 | Quantidade de backups preservados |

## 📊 Métricas Coletadas

### Performance Metrics
- **Timestamp** - Data/hora da requisição
- **Endpoint Path** - Rota acessada
- **HTTP Method** - GET, POST, PUT, DELETE
- **Status Code** - Código de resposta HTTP
- **Duration** - Tempo de resposta em milissegundos
- **CPU Usage** - Percentual de uso de CPU
- **Memory Usage** - Memória utilizada em bytes
- **Request ID** - Identificador único da requisição
- **User Identifier** - Usuário autenticado (se disponível)

### Error Metrics
- **Timestamp** - Data/hora do erro
- **Endpoint Path** - Onde ocorreu o erro
- **Exception Type** - Tipo da exceção
- **Exception Message** - Mensagem de erro
- **Stack Trace** - Stack trace completo
- **Request ID** - Identificador da requisição
- **User Identifier** - Usuário que causou o erro

## 🔌 APIs Disponíveis

### Métricas de Performance
```http
GET /api/metrics/performance?from=2024-01-01&to=2024-01-31
```

### Métricas de Erro
```http
GET /api/metrics/errors?from=2024-01-01&to=2024-01-31
```

### Resumo Consolidado
```http
GET /api/metrics/summary
```
Retorna métricas da última hora e último dia.

### Estatísticas do Banco
```http
GET /api/metrics/statistics
```
Retorna informações detalhadas sobre o banco de dados.

### Forçar Limpeza
```http
POST /api/metrics/cleanup?daysToKeep=30
```

## 🧪 Endpoints de Teste

### Teste Rápido
```http
GET /api/test/fast
```

### Teste com Delay
```http
GET /api/test/slow?delayMs=2000
```

### Gerar Erro
```http
GET /api/test/error
```

### Validação de Dados
```http
POST /api/test/validate
Content-Type: application/json

{
  "nome": "João Silva",
  "valor": 150,
  "email": "joao@exemplo.com"
}
```

## 📈 Exemplo de Resposta

### Performance Metrics
```json
[
  {
    "id": 1,
    "timestamp": "2024-01-15T10:30:00Z",
    "endpointPath": "/api/test/slow",
    "httpMethod": "GET",
    "statusCode": 200,
    "durationMs": 2043,
    "cpuUsagePercent": 15.73,
    "memoryUsageBytes": 112807936,
    "requestId": "0HND3PKK3OO3Q:00000001",
    "userIdentifier": null,
    "cpuUsageFormatted": "15.73%",
    "memoryUsageFormatted": "107.6 MB",
    "responseTimeFormatted": "2.04s"
  }
]
```

### Summary Response
```json
{
  "lastHour": {
    "requestCount": 45,
    "averageResponseTime": 245.5,
    "errorCount": 2,
    "averageCpuUsage": 12.3,
    "averageMemoryUsage": 98234567
  },
  "lastDay": {
    "requestCount": 1024,
    "averageResponseTime": 189.2,
    "errorCount": 15,
    "averageCpuUsage": 8.7,
    "averageMemoryUsage": 95123456
  }
}
```

## 🗂️ Sistema de Backup

O sistema cria backups automáticos do banco de dados:

- **Localização**: `./Logs/MetricsBackups/`
- **Formato**: `metrics_backup_YYYYMMDD_HHMMSS.db`
- **Frequência**: A cada execução da limpeza (6h)
- **Retenção**: 10 backups mais recentes

### Estrutura de Pastas
```
./Logs/MetricsBackups/
├── metrics_backup_20240115_143022.db
├── metrics_backup_20240115_083015.db
└── metrics_backup_20240114_203008.db
```

## 🔄 Background Services

### MetricsCleanupService
Executa a cada **6 horas** e realiza:

1. **Backup** do banco atual
2. **Limpeza por tamanho** - Remove 40% dos registros se > 100MB
3. **Limpeza por data** - Remove registros > 30 dias
4. **Limpeza de backups** - Mantém apenas 10 mais recentes
5. **VACUUM** do SQLite para recuperar espaço

## 🚀 Deploy e Produção

### Configurações Recomendadas

```json
{
  "FeatureFlags": {
    "DetailedPerformanceLogging": true
  },
  "MetricsSettings": {
    "CleanupIntervalHours": 6,
    "DaysToKeepData": 30,
    "MaxDatabaseSizeMB": 100,
    "MaxBackupsToKeep": 10
  }
}
```

### Considerações de Performance

- **Middleware leve** - Impacto mínimo na performance
- **Async/await** - Operações não-bloqueantes
- **Batch processing** - Limpeza em lotes de 1000 registros
- **Índices otimizados** - Consultas rápidas por data/endpoint

## 🛠️ Customização

### Adicionando Novas Métricas

1. **Estenda o modelo PerformanceMetric**:
```csharp
public string? AdditionalData { get; set; }
```

2. **Atualize o mapeamento no OnModelCreating**:
```csharp
entity.Property(p => p.AdditionalData)
    .HasMaxLength(500);
```

3. **Capture no middleware**:
```csharp
AdditionalData = GetCustomMetric(context)
```

### Configurando Intervalos Personalizados

Modifique o `MetricsCleanupService`:
```csharp
_cleanupInterval = TimeSpan.FromHours(12); // 12 horas
_daysToKeepData = 60; // 60 dias
```

## 📝 Logs e Debugging

Os logs são gerados automaticamente via `ILogger`:

```csharp
// Logs de performance
_logger.LogInformation("Limpeza de métricas iniciada...");

// Logs de erro
_logger.LogError(ex, "Erro ao registrar métrica de performance");
```

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add: Amazing Feature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🏷️ Tags

`asp.net-core` `dotnet` `monitoring` `logging` `metrics` `sqlite` `performance` `self-contained` `embedded-system` `api-monitoring`