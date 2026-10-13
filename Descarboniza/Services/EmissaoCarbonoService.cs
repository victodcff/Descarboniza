namespace Descarboniza.Services
{
    public class EmissaoCarbonoService
    {
        // Fator de emissão utilizado pelo sistema
        // Unidade: kg CO2 / kWh
        public const double FatorEmissaoPadrao = 0.07715;

        public double CalcularEmissao(double consumoKwh)
        {
            return consumoKwh * FatorEmissaoPadrao;
        }
    }
}