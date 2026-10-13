namespace Descarboniza.ViewModels;

public class DashboardMaquinaItem
{
    public int IdConsumo { get; set; }

    public string Fabricante { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public double ConsumoKwh { get; set; }

    public double CustoMensal { get; set; }

    public double EmissaoKgCo2 { get; set; }

    public double PercentualDoTotal { get; set; }

    public string Status { get; set; } = "indisponivel";
}

public class DashboardPainelViewModel
{
    public int QuantidadeMaquinas { get; set; }

    public int MaquinasComConsumo { get; set; }

    public double ConsumoMensal { get; set; }

    public double ConsumoMedio { get; set; }

    public double CustoMensal { get; set; }

    public double CustoAnual { get; set; }

    public double EmissaoMensalKgCo2 { get; set; }

    public int MaquinasAcimaDoTeorico { get; set; }

    public int MaquinasAbaixoDoTeorico { get; set; }

    public int MaquinasProximasDoTeorico { get; set; }

    public List<DashboardMaquinaItem> Maquinas { get; set; } = new();
}