using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class EmissaoCarbonoController : Controller
{
    private readonly DescarbonizaContext _context;
    private readonly EmissaoCarbonoService _emissaoService;

    public EmissaoCarbonoController(
        DescarbonizaContext context,
        EmissaoCarbonoService emissaoService)
    {
        _context = context;
        _emissaoService = emissaoService;
    }

    // GET: EmissaoCarbono
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var emissoes = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .Where(e =>
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId)
            .ToListAsync();

        return View(emissoes);
    }

    // GET: EmissaoCarbono/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var emissaoCarbono = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e =>
                e.IdEmissao == id &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (emissaoCarbono == null)
            return NotFound();

        return View(emissaoCarbono);
    }

    // GET: EmissaoCarbono/Create
    public async Task<IActionResult> Create()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m =>
                m.UsuarioId == usuarioId &&
                _context.Consumos.Any(c =>
                    c.IdMaquina == m.IdMaquina))
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        var emissao = new EmissaoCarbono
        {
            FatorEmissao = EmissaoCarbonoService.FatorEmissaoPadrao
        };

        return View(emissao);
    }

    // Busca consumo e calcula emissão automaticamente
    [HttpGet]
    public async Task<IActionResult> ObterDadosDaMaquina(int idMaquina)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquinaExiste = await _context.Maquinas
            .AnyAsync(m =>
                m.IdMaquina == idMaquina &&
                m.UsuarioId == usuarioId);

        if (!maquinaExiste)
        {
            return Json(new
            {
                encontrado = false
            });
        }

        var consumo = await _context.Consumos
            .Where(c =>
                c.IdMaquina == idMaquina &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .OrderByDescending(c => c.DataRegistro)
            .FirstOrDefaultAsync();

        if (consumo == null)
        {
            return Json(new
            {
                encontrado = false
            });
        }

        var emissao = _emissaoService.CalcularEmissao(
            consumo.ConsumoKwhMes);

        return Json(new
        {
            encontrado = true,
            consumoKwh = consumo.ConsumoKwhMes,
            fatorEmissao = EmissaoCarbonoService.FatorEmissaoPadrao,
            emissaoKgCo2 = emissao
        });
    }

    // POST: EmissaoCarbono/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdEmissao,IdMaquina,DataRegistro")]
        EmissaoCarbono emissaoCarbono)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verifica se a máquina pertence ao usuário
        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == emissaoCarbono.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }

        // Busca o consumo da máquina
        var consumo = await _context.Consumos
            .Where(c =>
                c.IdMaquina == emissaoCarbono.IdMaquina &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .OrderByDescending(c => c.DataRegistro)
            .FirstOrDefaultAsync();

        if (consumo == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não possui consumo cadastrado.");
        }

        // Verifica se a máquina já possui emissão
        var duplicado = await _context.EmissoesCarbono
            .AnyAsync(e =>
                e.IdMaquina == emissaoCarbono.IdMaquina &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (duplicado)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina já possui uma emissão de carbono cadastrada.");
        }

        emissaoCarbono.DataRegistro = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            emissaoCarbono.ConsumoKwh =
                consumo!.ConsumoKwhMes;

            emissaoCarbono.FatorEmissao =
                EmissaoCarbonoService.FatorEmissaoPadrao;

            emissaoCarbono.EmissaoKgCo2 =
                _emissaoService.CalcularEmissao(
                    consumo.ConsumoKwhMes);

            _context.EmissoesCarbono.Add(emissaoCarbono);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Emissão registrada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m =>
                m.UsuarioId == usuarioId &&
                _context.Consumos.Any(c =>
                    c.IdMaquina == m.IdMaquina))
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(emissaoCarbono);
    }

    // GET: EmissaoCarbono/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var emissaoCarbono = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e =>
                e.IdEmissao == id &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (emissaoCarbono == null)
            return NotFound();

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m =>
                m.UsuarioId == usuarioId &&
                _context.Consumos.Any(c =>
                    c.IdMaquina == m.IdMaquina))
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(emissaoCarbono);
    }

    // POST: EmissaoCarbono/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("IdEmissao,IdMaquina,DataRegistro")]
        EmissaoCarbono emissaoCarbono)
    {
        if (id != emissaoCarbono.IdEmissao)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Busca a emissão atual
        var emissaoBanco = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e =>
                e.IdEmissao == id &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (emissaoBanco == null)
            return NotFound();

        // Verifica se a máquina pertence ao usuário
        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == emissaoCarbono.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }

        // Busca o consumo da máquina
        var consumo = await _context.Consumos
            .Where(c =>
                c.IdMaquina == emissaoCarbono.IdMaquina &&
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .OrderByDescending(c => c.DataRegistro)
            .FirstOrDefaultAsync();

        if (consumo == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não possui consumo cadastrado.");
        }

        // Se estiver trocando de máquina, verifica se a nova máquina
        // já possui outra emissão
        var duplicado = await _context.EmissoesCarbono
            .AnyAsync(e =>
                e.IdEmissao != emissaoCarbono.IdEmissao &&
                e.IdMaquina == emissaoCarbono.IdMaquina &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (duplicado)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina já possui uma emissão de carbono cadastrada.");
        }

        emissaoCarbono.DataRegistro = emissaoBanco.DataRegistro;

        if (ModelState.IsValid)
        {
            emissaoBanco.IdMaquina =
                emissaoCarbono.IdMaquina;

            emissaoBanco.DataRegistro =
                emissaoCarbono.DataRegistro;

            emissaoBanco.ConsumoKwh =
                consumo!.ConsumoKwhMes;

            emissaoBanco.FatorEmissao =
                EmissaoCarbonoService.FatorEmissaoPadrao;

            emissaoBanco.EmissaoKgCo2 =
                _emissaoService.CalcularEmissao(
                    consumo.ConsumoKwhMes);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Emissão atualizada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await _context.Maquinas
            .Where(m =>
                m.UsuarioId == usuarioId &&
                _context.Consumos.Any(c =>
                    c.IdMaquina == m.IdMaquina))
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(emissaoCarbono);
    }

    // GET: EmissaoCarbono/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var emissaoCarbono = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e =>
                e.IdEmissao == id &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (emissaoCarbono == null)
            return NotFound();

        return View(emissaoCarbono);
    }

    // POST: EmissaoCarbono/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var emissaoCarbono = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e =>
                e.IdEmissao == id &&
                e.Maquina != null &&
                e.Maquina.UsuarioId == usuarioId);

        if (emissaoCarbono == null)
            return NotFound();

        _context.EmissoesCarbono.Remove(emissaoCarbono);

        await _context.SaveChangesAsync();

        TempData["Excluido"] = "Emissão excluída com sucesso.";

        return RedirectToAction(nameof(Index));
    }
}