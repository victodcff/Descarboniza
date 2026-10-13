using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Descarboniza.Areas.Identity.Data
{
    public class Consumo
    {
        [Key]
        public int IdConsumo { get; set; }

        public int IdMaquina { get; set; }

        [ForeignKey("IdMaquina")]
        public Maquina? Maquina { get; set; }

        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

        public double HorasUsoDia { get; set; }

        public double DiasUsoMes { get; set; }

        public double ConsumoKwhMes { get; set; }

        public double TarifaKwh { get; set; }
    }
}