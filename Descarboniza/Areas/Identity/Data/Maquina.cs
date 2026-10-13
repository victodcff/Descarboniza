using System.ComponentModel.DataAnnotations;

namespace Descarboniza.Areas.Identity.Data
{
    public class Maquina
    {
        [Key]
        public int IdMaquina { get; set; }

        public string Fabricante { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public double Potencia { get; set; }
        public double Tensao { get; set; }
        public double Corrente { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public string? UsuarioId { get; set; }
    }
}