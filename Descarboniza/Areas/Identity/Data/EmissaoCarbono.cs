using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Descarboniza.Areas.Identity.Data
{
    public class EmissaoCarbono
    {
        [Key]
        public int IdEmissao { get; set; }

        public int IdMaquina { get; set; }

        [ForeignKey("IdMaquina")]
        public Maquina? Maquina { get; set; }

        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

        public double ConsumoKwh { get; set; }

        public double FatorEmissao { get; set; }

        public double EmissaoKgCo2 { get; set; }
    }
}