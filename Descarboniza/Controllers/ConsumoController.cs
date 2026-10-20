using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class ConsumoController : Controller
{
    private readonly DescarbonizaContext _context;
    private readonly AnaliseConsumoService _analiseService;
    private readonly EmissaoCarbonoService _emissaoService;

    public ConsumoController(
        DescarbonizaContext context,
        AnaliseConsumoService analiseService,
        EmissaoCarbonoService emissaoService)
    {
        _context = context;
        _analiseService = analiseService;
        _emissaoService = emissaoService;
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

        ViewBag.Maquinas = await ObterMaquinasAsync(usuarioId);

        return View();
    }

    // POST: Consumo/Create
    // O consumo mensal NÃO vem do formulário: é calculado aqui, a partir da
    // potência da máquina, das horas por dia e dos dias por mês.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdConsumo,IdMaquina,DataRegistro,HorasUsoDia,DiasUsoMes,TarifaKwh")]
        Consumo consumo)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Busca a máquina garantindo que pertence ao usuário
        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == consumo.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }
        else if (maquina.Potencia <= 0)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina está sem potência cadastrada. Edite a máquina antes de registrar o consumo.");
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

        ValidarUso(consumo);

        consumo.DataRegistro = DateTime.UtcNow;

        if (ModelState.IsValid && maquina != null)
        {
            consumo.ConsumoKwhMes = _analiseService.CalcularConsumoMensal(
                maquina.Potencia,
                consumo.HorasUsoDia,
                consumo.DiasUsoMes);

            _context.Consumos.Add(consumo);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Consumo registrado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await ObterMaquinasAsync(usuarioId);

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

        ViewBag.Maquinas = await ObterMaquinasAsync(usuarioId);

        return View(consumo);
    }

    // POST: Consumo/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("IdConsumo,IdMaquina,DataRegistro,HorasUsoDia,DiasUsoMes,TarifaKwh")]
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

        // Busca a máquina escolhida garantindo que pertence ao usuário
        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == consumo.IdMaquina &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "A máquina selecionada não pertence ao usuário.");
        }
        else if (maquina.Potencia <= 0)
        {
            ModelState.AddModelError(
                "IdMaquina",
                "Esta máquina está sem potência cadastrada. Edite a máquina antes de registrar o consumo.");
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

        ValidarUso(consumo);

        consumo.DataRegistro = DateTime.UtcNow;

        if (ModelState.IsValid && maquina != null)
        {
            consumoBanco.IdMaquina = consumo.IdMaquina;
            consumoBanco.DataRegistro = consumo.DataRegistro;
            consumoBanco.HorasUsoDia = consumo.HorasUsoDia;
            consumoBanco.DiasUsoMes = consumo.DiasUsoMes;
            consumoBanco.TarifaKwh = consumo.TarifaKwh;

            consumoBanco.ConsumoKwhMes = _analiseService.CalcularConsumoMensal(
                maquina.Potencia,
                consumoBanco.HorasUsoDia,
                consumoBanco.DiasUsoMes);

            // Se a máquina já tem emissão registrada, mantém ela em dia
            var emissao = await _context.EmissoesCarbono
                .FirstOrDefaultAsync(e => e.IdMaquina == consumoBanco.IdMaquina);

            if (emissao != null)
            {
                emissao.ConsumoKwh = consumoBanco.ConsumoKwhMes;
                emissao.FatorEmissao = EmissaoCarbonoService.FatorEmissaoPadrao;
                emissao.EmissaoKgCo2 =
                    _emissaoService.CalcularEmissao(consumoBanco.ConsumoKwhMes);
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Consumo atualizado com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Maquinas = await ObterMaquinasAsync(usuarioId);

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
            .ToListAsync();

        var total = consumos.Sum(c => c.ConsumoKwhMes);

        // Do maior para o menor consumo: a máquina que mais pesa fica no topo
        var lista = consumos
            .OrderByDescending(c => c.ConsumoKwhMes)
            .Select(c => new ResumoConsumoViewModel
            {
                IdConsumo = c.IdConsumo,
                Fabricante = c.Maquina?.Fabricante ?? string.Empty,
                Modelo = c.Maquina?.Modelo ?? string.Empty,
                ConsumoKwhMes = c.ConsumoKwhMes,
                CustoMensal = _analiseService.CalcularCustoMensal(c),
                EmissaoKgCo2 = _emissaoService.CalcularEmissao(c.ConsumoKwhMes),
                ParticipacaoPercentual = total > 0
                    ? c.ConsumoKwhMes / total * 100
                    : 0
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

        var totalDoUsuario = await _context.Consumos
            .Where(c =>
                c.Maquina != null &&
                c.Maquina.UsuarioId == usuarioId)
            .SumAsync(c => c.ConsumoKwhMes);

        var emissaoMensal =
            _emissaoService.CalcularEmissao(consumo.ConsumoKwhMes);

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
                _analiseService.ClassificarConsumo(consumo),

            EmissaoMensal = emissaoMensal,

            EmissaoAnual = emissaoMensal * 12,

            ParticipacaoPercentual = totalDoUsuario > 0
                ? consumo.ConsumoKwhMes / totalDoUsuario * 100
                : 0
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

    private Task<List<Maquina>> ObterMaquinasAsync(string? usuarioId)
    {
        return _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();
    }

    // O formulário não valida no navegador (data-val="false"), então os
    // limites são conferidos aqui. Não repete a mensagem quando o campo
    // já veio com erro de formato (por exemplo, texto no lugar de número).
    private void ValidarUso(Consumo consumo)
    {
        if (Valido(nameof(Consumo.HorasUsoDia)) &&
            (consumo.HorasUsoDia <= 0 || consumo.HorasUsoDia > 24))
        {
            ModelState.AddModelError(
                nameof(Consumo.HorasUsoDia),
                "Informe de 0,1 a 24 horas por dia.");
        }

        if (Valido(nameof(Consumo.DiasUsoMes)) &&
            (consumo.DiasUsoMes < 1 || consumo.DiasUsoMes > 31))
        {
            ModelState.AddModelError(
                nameof(Consumo.DiasUsoMes),
                "Informe de 1 a 31 dias por mês.");
        }

        if (Valido(nameof(Consumo.TarifaKwh)) && consumo.TarifaKwh <= 0)
        {
            ModelState.AddModelError(
                nameof(Consumo.TarifaKwh),
                "Informe uma tarifa maior que zero.");
        }
    }

    private bool Valido(string campo)
    {
        return ModelState.GetFieldValidationState(campo) != ModelValidationState.Invalid;
    }
}
