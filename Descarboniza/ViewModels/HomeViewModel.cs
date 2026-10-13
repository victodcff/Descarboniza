namespace Descarboniza.ViewModels
{
    public class HomeViewModel
    {
        public string NomeUsuario { get; set; } = "Usuário";

        public int QuantidadeMaquinas { get; set; }

        public double ConsumoMensal { get; set; }

        public double CustoMensal { get; set; }

        public int QuantidadeConsumos { get; set; }

        public int QuantidadeEmissoes { get; set; }
    }
}