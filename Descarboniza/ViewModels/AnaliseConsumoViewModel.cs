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

        // Emissão de CO2 estimada a partir do consumo mensal
        public double EmissaoMensal { get; set; }

        public double EmissaoAnual { get; set; }

        // Quanto esta máquina representa do consumo total do usuário (%)
        public double ParticipacaoPercentual { get; set; }
    }
}