using Descarboniza.Areas.Identity.Data;

namespace Descarboniza.ViewModels
{
    public class AnaliseConsumoViewModel
    {
        public Consumo Consumo { get; set; } = null!;

        public double ConsumoTeorico { get; set; }

        public double ConsumoAnual { get; set; }

        public double ConsumoDiario { get; set; }

        public double ConsumoPorHora { get; set; }

        public double CustoMensal { get; set; }

        public double CustoAnual { get; set; }

        public double CustoDiario { get; set; }

        public double CustoPorHora { get; set; }

        public double DiferencaConsumo { get; set; }

        public double PercentualDiferenca { get; set; }

        public string Classificacao { get; set; } = string.Empty;
    }
}