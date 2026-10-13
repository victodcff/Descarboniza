namespace Descarboniza.ViewModels
{
    public class DashboardViewModel
    {
        public int QuantidadeMaquinas { get; set; }

        public double ConsumoMensal { get; set; }

        public double CustoMensal { get; set; }

        public double CustoAnual { get; set; }

        public List<ConsumoMaquinaViewModel> ConsumoPorMaquina { get; set; } = new();
    }

    public class ConsumoMaquinaViewModel
    {
        public string NomeMaquina { get; set; } = string.Empty;

        public double ConsumoKwh { get; set; }
    }
}