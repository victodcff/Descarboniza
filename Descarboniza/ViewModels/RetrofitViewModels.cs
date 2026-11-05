namespace Descarboniza.ViewModels
{
    /// <summary>Dados da máquina enviados para a IA (nomes em camelCase no JSON).</summary>
    public class RetrofitRequest
    {
        public string Fabricante { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public double PotenciaKw { get; set; }
        public double? TensaoV { get; set; }
        public double? CorrenteA { get; set; }
        public double HorasUsoDia { get; set; }
        public double DiasUsoMes { get; set; }
        public double ConsumoMensalKwh { get; set; }
        public double CustoMensalReais { get; set; }
        public double EmissaoMensalKg { get; set; }
        public double? ParticipacaoPercentual { get; set; }
    }

    /// <summary>Resposta da IA (POST /sugerir-retrofit).</summary>
    public class RetrofitResponse
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public string? Resumo { get; set; }
        public List<RetrofitSugestao> Sugestoes { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }

    public class RetrofitSugestao
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Impacto { get; set; } = string.Empty;
        public string Esforco { get; set; } = string.Empty;
        public string VerificarAntes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simulação calculada pelo site (não pela IA): "e se o consumo caísse X%?".
    /// </summary>
    public class RetrofitCenario
    {
        public int ReducaoPercentual { get; set; }
        public double EconomiaKwhMes { get; set; }
        public double EconomiaReaisMes { get; set; }
        public double EconomiaReaisAno { get; set; }
        public double EconomiaCo2KgAno { get; set; }
    }
}
