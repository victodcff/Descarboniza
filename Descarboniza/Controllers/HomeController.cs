using Descarboniza.Areas.Identity.Data;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class HomeController : Controller
{
    private readonly DescarbonizaContext _context;

    public HomeController(DescarbonizaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Se estiver logado, carrega os dados do usuário
        if (User.Identity?.IsAuthenticated == true)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var maquinas = await _context.Maquinas
                .Where(m => m.UsuarioId == usuarioId)
                .ToListAsync();

            var consumos = await _context.Consumos
                .Include(c => c.Maquina)
                .Where(c => c.Maquina != null &&
                            c.Maquina.UsuarioId == usuarioId)
                .ToListAsync();

            var consumoMensal = consumos.Sum(c => c.ConsumoKwhMes);

            var custoMensal = consumos.Sum(c =>
                c.ConsumoKwhMes * c.TarifaKwh);

            var model = new DashboardViewModel
            {
                QuantidadeMaquinas = maquinas.Count,

                ConsumoMensal = consumoMensal,

                CustoMensal = custoMensal,

                CustoAnual = custoMensal * 12,

                ConsumoPorMaquina = consumos
                    .GroupBy(c => c.IdMaquina)
                    .Select(g => new ConsumoMaquinaViewModel
                    {
                        NomeMaquina =
                            g.First().Maquina!.Fabricante +
                            " - " +
                            g.First().Maquina!.Modelo,

                        ConsumoKwh = g.Sum(c => c.ConsumoKwhMes)
                    })
                    .ToList()
            };

            return View(model);
        }

        // Se estiver deslogado, mostra a Home pública
        return View(new DashboardViewModel());
    }

    public IActionResult Privacy()
    {
        return View();
    }
}