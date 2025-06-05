using System.ComponentModel.DataAnnotations;

namespace LogEmbarcado.API.Dtos
{
    public class TestPerformanceDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;


        [Range(1, 1000, ErrorMessage = "Valor deve estar entre 1 e 1000")]
        public int Valor { get; set; }


        [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
        public string? Email { get; set; }
    }
}
