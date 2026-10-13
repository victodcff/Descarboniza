using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class ConsumoController : Controller
{
    private readonly DescarbonizaContext _context;
    private readonly AnaliseConsumoService _analiseService;

    public ConsumoController(
        DescarbonizaContext context,
        AnaliseConsumoService analiseService)
    {
        _context = context;
        _analiseService = analiseService;
    }

    // GET: Consumo
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumos = await _context.Consumos
            .Include(c => c.Maquina)
            .Where(c =>
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .ToListAsync();

        return View(consumos);
    }

    // GET: Consumo/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumo = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumo == null)
            return NotFound();

        return View(consumo);
    }

    // GET: Consumo/Create
    public async Task<IActionResult> Create()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View();
    }

    // POST: Consumo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdConsumo,IdMaquina,DataRegistro,HorasUsoDia,DiasUsoMes,ConsumoKwhMes,TarifaKwh")]
        Consumo consumo)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verifica se a máquina pertence ao usuário
        var maquinaPertenceAoUsuario = await _context.Maquinas
            .AnyAsync(m =>
                m.IdMaquina == consumo.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (!maquinaPertenceAoUsuario)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }

        // Verifica se a máquina já possui consumo
        var duplicado = await _context.Consumos
            .AnyAsync(c =>
                c.IdMaquina == consumo.IdMaquina &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (duplicado)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina já possui um consumo cadastrado.");
        }

        consumo.DataRegistro = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            _context.Consumos.Add(consumo);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Consumo registrado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(consumo);
    }

    // GET: Consumo/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumo = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumo == null)
            return NotFound();

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(consumo);
    }

    // POST: Consumo/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("IdConsumo,IdMaquina,DataRegistro,HorasUsoDia,DiasUsoMes,ConsumoKwhMes,TarifaKwh")]
        Consumo consumo)
    {
        if (id != consumo.IdConsumo)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Busca o consumo atual garantindo que pertence ao usuário
        var consumoBanco = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumoBanco == null)
            return NotFound();

        // Verifica se a máquina pertence ao usuário
        var maquinaPertenceAoUsuario = await _context.Maquinas
            .AnyAsync(m =>
                m.IdMaquina == consumo.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (!maquinaPertenceAoUsuario)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }

        // Se estiver trocando de máquina, verifica se a nova máquina
        // já possui outro consumo
        var duplicado = await _context.Consumos
            .AnyAsync(c =>
                c.IdConsumo != consumo.IdConsumo &&
                c.IdMaquina == consumo.IdMaquina &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (duplicado)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina já possui um consumo cadastrado.");
        }

        consumo.DataRegistro = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            consumoBanco.IdMaquina = consumo.IdMaquina;
            consumoBanco.DataRegistro = consumo.DataRegistro;
            consumoBanco.HorasUsoDia = consumo.HorasUsoDia;
            consumoBanco.DiasUsoMes = consumo.DiasUsoMes;
            consumoBanco.ConsumoKwhMes = consumo.ConsumoKwhMes;
            consumoBanco.TarifaKwh = consumo.TarifaKwh;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Consumo atualizado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(consumo);
    }

    // GET: Consumo/Analises
    public async Task<IActionResult> Analises()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumos = await _context.Consumos
            .Include(c => c.Maquina)
            .Where(c =>
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .OrderBy(c => c.Maquina!.Fabricante)
            .ThenBy(c => c.Maquina!.Modelo)
            .ToListAsync();

        var lista = consumos
            .Select(c =>
            {
                var teorico = _analiseService.CalcularConsumoTeorico(c);
                var percentual = _analiseService.CalcularPercentualDiferenca(c);

                return new AnaliseResumoViewModel
                {
                    IdConsumo = c.IdConsumo,
                    Fabricante = c.Maquina?.Fabricante ?? string.Empty,
                    Modelo = c.Maquina?.Modelo ?? string.Empty,
                    ConsumoInformado = c.ConsumoKwhMes,
                    ConsumoTeorico = teorico,
                    PercentualDiferenca = percentual,
                    Status = AnaliseStatus.Obter(teorico, percentual)
                };
            })
            .ToList();

        return View(lista);
    }

    // GET: Consumo/Analise/5
    public async Task<IActionResult> Analise(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumo = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumo == null)
            return NotFound();

        var resultado = new AnaliseConsumoViewModel
        {
            Consumo = consumo,

            ConsumoTeorico =
                _analiseService.CalcularConsumoTeorico(consumo),

            ConsumoAnual =
                _analiseService.CalcularConsumoAnual(consumo),

            ConsumoDiario =
                _analiseService.CalcularConsumoDiario(consumo),

            ConsumoPorHora =
                _analiseService.CalcularConsumoPorHora(consumo),

            CustoMensal =
                _analiseService.CalcularCustoMensal(consumo),

            CustoAnual =
                _analiseService.CalcularCustoAnual(consumo),

            CustoDiario =
                _analiseService.CalcularCustoDiario(consumo),

            CustoPorHora =
                _analiseService.CalcularCustoPorHora(consumo),

            DiferencaConsumo =
                _analiseService.CalcularDiferencaConsumo(consumo),

            PercentualDiferenca =
                _analiseService.CalcularPercentualDiferenca(consumo),

            Classificacao =
                _analiseService.ClassificarConsumo(consumo)
        };

        return View(resultado);
    }

    // GET: Consumo/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumo = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumo == null)
            return NotFound();

        return View(consumo);
    }

    // POST: Consumo/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var consumo = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c =>
                c.IdConsumo == id &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId);

        if (consumo == null)
            return NotFound();

        _context.Consumos.Remove(consumo);

        await _context.SaveChangesAsync();

        TempData["Excluido"] = "Consumo excluído com sucesso.";

        return RedirectToAction(nameof(Index));
    }
}