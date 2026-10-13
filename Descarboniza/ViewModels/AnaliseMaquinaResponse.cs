namespace Descarboniza.ViewModels
{
    public class AnaliseMaquinaResponse
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public string? TipoMaquina { get; set; }
        public string? Fabricante { get; set; }
        public string? Modelo { get; set; }
        public double? PotenciaKw { get; set; }
        public double? TensaoV { get; set; }
        public double? CorrenteA { get; set; }
        public List<ConfiguracaoEletricaResponse> Configuracoes { get; set; } = new();
        public List<string> OutrasInformacoes { get; set; } = new();
        public List<string> CamposNaoIdentificados { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }

    public class ConfiguracaoEletricaResponse
    {
        public double? TensaoV { get; set; }
        public double? CorrenteA { get; set; }
        public string? Descricao { get; set; }
    }
}