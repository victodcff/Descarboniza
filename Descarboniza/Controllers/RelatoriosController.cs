using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

[Authorize]
public class RelatoriosController : Controller
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly DescarbonizaContext _context;
    private readonly AnaliseConsumoService _analiseService;
    private readonly EmissaoCarbonoService _emissaoService;

    public RelatoriosController(
        DescarbonizaContext context,
        AnaliseConsumoService analiseService,
        EmissaoCarbonoService emissaoService)
    {
        _context = context;
        _analiseService = analiseService;
        _emissaoService = emissaoService;
    }

    // GET: Relatorios
    public async Task<IActionResult> Index()
    {
        return View(await MontarRelatorioAsync());
    }

    // GET: Relatorios/Csv  (abre direto no Excel, com acentos e vírgula decimal)
    public async Task<IActionResult> Csv(string? ids)
    {
        var r = await MontarRelatorioAsync(LerIds(ids));
        var sb = new StringBuilder();

        sb.AppendLine("Máquina;Horas de uso/dia;Dias de uso/mês;Consumo (kWh/mês);Tarifa (R$/kWh);Custo mensal (R$);Emissão (kg CO2/mês);% do consumo total");

        foreach (var l in r.Linhas)
        {
            sb.AppendLine(string.Join(';',
                Texto(l.Maquina),
                Texto(Descarboniza.Helpers.FormatoHelper.Horas(l.HorasUsoDia)),
                Num(l.DiasUsoMes),
                Num(l.ConsumoKwhMes),
                Num(l.TarifaKwh, 4),
                Num(l.CustoMensal),
                Num(l.EmissaoKgCo2),
                Num(l.ParticipacaoPercentual)));
        }

        sb.AppendLine(string.Join(';',
            "TOTAL", "", "",
            Num(r.TotalKwhMes), "",
            Num(r.TotalCustoMes),
            Num(r.TotalEmissaoMes),
            r.Linhas.Count > 0 ? Num(100) : ""));

        // BOM UTF-8 para o Excel reconhecer os acentos
        var bytes = new UTF8Encoding(true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        var nome = $"relatorio-descarboniza-{DateTime.Now:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", nome);
    }

    private async Task<RelatorioViewModel> MontarRelatorioAsync(HashSet<int>? filtroIds = null)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumos = await _context.Consumos
            .Include(c => c.Maquina)
            .Where(c => c.Maquina != null && c.Maquina.UsuarioId == usuarioId)
            .ToListAsync();

        // Todas as máquinas do usuário (para os filtros da tela)
        var todas = consumos
            .OrderByDescending(c => c.ConsumoKwhMes)
            .Select(c => new RelatorioOpcao
            {
                IdConsumo = c.IdConsumo,
                Nome = $"{c.Maquina?.Fabricante} {c.Maquina?.Modelo}".Trim(),
                ConsumoKwhMes = c.ConsumoKwhMes,
                CustoMensal = _analiseService.CalcularCustoMensal(c)
            })
            .ToList();

        // Filtro: só as máquinas escolhidas (o usuário nunca vê dados de outros)
        if (filtroIds != null && filtroIds.Count > 0)
            consumos = consumos.Where(c => filtroIds.Contains(c.IdConsumo)).ToList();

        var total = consumos.Sum(c => c.ConsumoKwhMes);

        var linhas = consumos
            .OrderByDescending(c => c.ConsumoKwhMes)
            .Select(c => new RelatorioLinha
            {
                IdConsumo = c.IdConsumo,
                Maquina = $"{c.Maquina?.Fabricante} {c.Maquina?.Modelo}".Trim(),
                HorasUsoDia = c.HorasUsoDia,
                DiasUsoMes = c.DiasUsoMes,
                ConsumoKwhMes = c.ConsumoKwhMes,
                TarifaKwh = c.TarifaKwh,
                CustoMensal = _analiseService.CalcularCustoMensal(c),
                EmissaoKgCo2 = _emissaoService.CalcularEmissao(c.ConsumoKwhMes),
                ParticipacaoPercentual = total > 0 ? c.ConsumoKwhMes / total * 100 : 0
            })
            .ToList();

        return new RelatorioViewModel
        {
            NomeUsuario = User.Identity?.Name ?? string.Empty,
            GeradoEm = DateTime.Now,
            Opcoes = todas,
            Linhas = linhas,
            TotalKwhMes = linhas.Sum(l => l.ConsumoKwhMes),
            TotalCustoMes = linhas.Sum(l => l.CustoMensal),
            TotalEmissaoMes = linhas.Sum(l => l.EmissaoKgCo2)
        };
    }

    // "3,5,9" -> {3,5,9}; ignora lixo
    private static HashSet<int>? LerIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return null;

        var lista = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToHashSet();

        return lista.Count > 0 ? lista : null;
    }

    private static string Num(double valor, int casas = 2)
        => valor.ToString("F" + casas, PtBr);

    // Protege contra "injeção de fórmula" no Excel e escapa aspas/;
    private static string Texto(string valor)
    {
        if (valor.Length > 0 && "=+-@".Contains(valor[0]))
            valor = "'" + valor;

        return "\"" + valor.Replace("\"", "\"\"") + "\"";
    }
}
