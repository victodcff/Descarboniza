using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class DashboardController : Controller
{
    private readonly DescarbonizaContext _context;
    private readonly AnaliseConsumoService _analiseService;
    private readonly EmissaoCarbonoService _emissaoService;

    public DashboardController(
        DescarbonizaContext context,
        AnaliseConsumoService analiseService,
        EmissaoCarbonoService emissaoService)
    {
        _context = context;
        _analiseService = analiseService;
        _emissaoService = emissaoService;
    }

    // GET: Dashboard
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var quantidadeMaquinas = await _context.Maquinas
            .CountAsync(m => m.UsuarioId == usuarioId);

        var consumos = await _context.Consumos
            .Include(c => c.Maquina)
            .Where(c =>
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .ToListAsync();

        // Um consumo por máquina: o mais recente
        var maisRecentes = consumos
            .GroupBy(c => c.IdMaquina)
            .Select(g => g.OrderByDescending(c => c.DataRegistro).First())
            .ToList();

        var itens = new List<DashboardMaquinaItem>();

        foreach (var consumo in maisRecentes)
        {
            var teorico = _analiseService.CalcularConsumoTeorico(consumo);
            var percentual = _analiseService.CalcularPercentualDiferenca(consumo);

            itens.Add(new DashboardMaquinaItem
            {
                IdConsumo = consumo.IdConsumo,
                Fabricante = consumo.Maquina?.Fabricante ?? string.Empty,
                Modelo = consumo.Maquina?.Modelo ?? string.Empty,
                ConsumoKwh = consumo.ConsumoKwhMes,
                CustoMensal = _analiseService.CalcularCustoMensal(consumo),
                EmissaoKgCo2 = _emissaoService.CalcularEmissao(consumo.ConsumoKwhMes),
                Status = AnaliseStatus.Obter(teorico, percentual)
            });
        }

        var consumoTotal = itens.Sum(i => i.ConsumoKwh);

        foreach (var item in itens)
        {
            item.PercentualDoTotal = consumoTotal > 0
                ? item.ConsumoKwh / consumoTotal * 100
                : 0;
        }

        itens = itens
            .OrderByDescending(i => i.ConsumoKwh)
            .ToList();

        var custoMensal = itens.Sum(i => i.CustoMensal);

        var modelo = new DashboardPainelViewModel
        {
            QuantidadeMaquinas = quantidadeMaquinas,
            MaquinasComConsumo = itens.Count,
            ConsumoMensal = consumoTotal,
            ConsumoMedio = itens.Count > 0 ? consumoTotal / itens.Count : 0,
            CustoMensal = custoMensal,
            CustoAnual = custoMensal * 12,
            EmissaoMensalKgCo2 = itens.Sum(i => i.EmissaoKgCo2),
            MaquinasAcimaDoTeorico = itens.Count(i => i.Status == "alerta"),
            MaquinasAbaixoDoTeorico = itens.Count(i => i.Status == "abaixo"),
            MaquinasProximasDoTeorico = itens.Count(i => i.Status == "proximo"),
            Maquinas = itens
        };

        return View(modelo);
    }
}