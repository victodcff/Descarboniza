namespace Descarboniza.ViewModels
{
    // Dados da tela "Relatórios": totais do usuário + uma linha por máquina.
    public class RelatorioViewModel
    {
        public string NomeUsuario { get; set; } = string.Empty;

        public DateTime GeradoEm { get; set; }

        public double TotalKwhMes { get; set; }

        public double TotalCustoMes { get; set; }

        public double TotalEmissaoMes { get; set; }

        public List<RelatorioLinha> Linhas { get; set; } = new();

        // Todas as máquinas, para montar o filtro
        public List<RelatorioOpcao> Opcoes { get; set; } = new();
    }

    public class RelatorioOpcao
    {
        public int IdConsumo { get; set; }

        public string Nome { get; set; } = string.Empty;

        public double ConsumoKwhMes { get; set; }

        public double CustoMensal { get; set; }
    }

    public class RelatorioLinha
    {
        public int IdConsumo { get; set; }

        public string Maquina { get; set; } = string.Empty;

        public double HorasUsoDia { get; set; }

        public double DiasUsoMes { get; set; }

        public double ConsumoKwhMes { get; set; }

        public double TarifaKwh { get; set; }

        public double CustoMensal { get; set; }

        public double EmissaoKgCo2 { get; set; }

        public double ParticipacaoPercentual { get; set; }
    }
}
