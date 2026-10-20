namespace Descarboniza.ViewModels
{
    // Uma linha da tela "Análises": consumo, custo e emissão de cada máquina.
    public class ResumoConsumoViewModel
    {
        public int IdConsumo { get; set; }

        public string Fabricante { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public double ConsumoKwhMes { get; set; }

        public double CustoMensal { get; set; }

        public double EmissaoKgCo2 { get; set; }

        public double ParticipacaoPercentual { get; set; }
    }
}
