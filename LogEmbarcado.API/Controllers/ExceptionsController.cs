using Microsoft.AspNetCore.Mvc;

namespace LogEmbarcado.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExceptionsController : ControllerBase
    {
        [HttpGet("null-reference")]
        public IActionResult GetNullReference()
        {
            string? texto = null;
            var resultado = texto.Length;
            return Ok(resultado);
        }

        [HttpPost("argument-exception")]
        public IActionResult PostArgumentException([FromBody] object data)
        {
            throw new ArgumentException("Argumento inválido fornecido", nameof(data));
        }

        [HttpPut("invalid-operation")]
        public IActionResult PutInvalidOperation()
        {
            var lista = new List<int>();
            var primeiro = lista.First();
            return Ok(primeiro);
        }

        [HttpDelete("not-implemented")]
        public IActionResult DeleteNotImplemented()
        {
            throw new NotImplementedException("Este método ainda não foi implementado");
        }
    }
}
