using LogEmbarcado.API.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace LogEmbarcado.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceController : ControllerBase
    {
        private readonly Random _random = new();

        [HttpGet("performance-test")]
        public async Task<IActionResult> GetPerformanceTest([FromQuery] int delayMs = 1000)
        {
            await Task.Delay(delayMs);

            var resultado = new
            {
                Timestamp = DateTime.UtcNow,
                DelaySimulado = delayMs,
                NumeroAleatorio = _random.Next(1, 1000),
                Servidor = Environment.MachineName,
                Mensagem = $"Processamento concluído após {delayMs}ms"
            };

            return Ok(resultado);
        }

        [HttpPost("data-processing")]
        public async Task<IActionResult> PostDataProcessing([FromBody] TestPerformanceDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var delayBase = Math.Max(500, request.Valor * 2);
            var delayVariacao = _random.Next(0, 1000);
            var delayTotal = delayBase + delayVariacao;

            await Task.Delay(delayTotal);

            // Simula algum processamento adicional
            var processedData = new
            {
                Id = Guid.NewGuid(),
                NomeProcessado = request.Nome.ToUpper(),
                ValorCalculado = request.Valor * 1.5,
                EmailValidado = !string.IsNullOrEmpty(request.Email),
                TempoProcessamento = delayTotal,
                TimestampInicio = DateTime.UtcNow.AddMilliseconds(-delayTotal),
                TimestampFim = DateTime.UtcNow,
                Status = "Processado com sucesso"
            };

            return Ok(processedData);
        }
    }
}
