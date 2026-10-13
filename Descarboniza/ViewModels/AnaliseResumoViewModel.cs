namespace Descarboniza.ViewModels;

public static class AnaliseStatus
{
    // Mesma tolerância de 10% usada em AnaliseConsumoService.ClassificarConsumo
    private const double Tolerancia = 10;

    public static string Obter(double consumoTeorico, double percentualDiferenca)
    {
        if (consumoTeorico <= 0)
            return "indisponivel";

        if (percentualDiferenca < -Tolerancia)
            return "alerta";

        if (percentualDiferenca > Tolerancia)
            return "abaixo";

        return "proximo";
    }
}

public class AnaliseResumoViewModel
{
    public int IdConsumo { get; set; }

    public string Fabricante { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public double ConsumoInformado { get; set; }

    public double ConsumoTeorico { get; set; }

    public double PercentualDiferenca { get; set; }

    public string Status { get; set; } = "indisponivel";
}