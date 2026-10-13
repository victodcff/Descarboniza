using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;

[Authorize]
public class MaquinaController : Controller
{
    private static readonly string[] TiposImagemAceitos =
        { "image/jpeg", "image/png", "image/webp" };

    private const long TamanhoMaximoImagem = 5 * 1024 * 1024;

    private readonly DescarbonizaContext _context;
    private readonly IaMaquinaService _ia;

    public MaquinaController(DescarbonizaContext context, IaMaquinaService ia)
    {
        _context = context;
        _ia = ia;
    }

    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .ToListAsync();

        return View(maquinas);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == id &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
            return NotFound();

        return View(maquina);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdMaquina,Fabricante,Modelo,Potencia,Tensao,Corrente,DataCadastro")]
        Maquina maquina)
    {
        if (ModelState.IsValid)
        {
            maquina.UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            maquina.DataCadastro = DateTime.UtcNow;

            _context.Add(maquina);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Máquina cadastrada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        return View(maquina);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> AnalisarImagem(
        IFormFile? imagem,
        CancellationToken ct)
    {
        if (imagem == null || imagem.Length == 0)
        {
            return BadRequest(new AnaliseMaquinaResponse
            {
                Erro = "Selecione uma imagem."
            });
        }

        if (imagem.Length > TamanhoMaximoImagem)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new AnaliseMaquinaResponse { Erro = "Imagem maior que 5 MB." });
        }

        if (!TiposImagemAceitos.Contains(imagem.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new AnaliseMaquinaResponse { Erro = "Use uma imagem JPG, PNG ou WEBP." });
        }

        var resultado = await _ia.AnalisarAsync(imagem, ct);

        return Json(resultado);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == id &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
            return NotFound();

        return View(maquina);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("IdMaquina,Fabricante,Modelo,Potencia,Tensao,Corrente,DataCadastro")]
        Maquina maquina)
    {
        if (id != maquina.IdMaquina)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquinaBanco = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == id &&
                m.UsuarioId == usuarioId);

        if (maquinaBanco == null)
            return NotFound();

        maquina.DataCadastro = maquinaBanco.DataCadastro;

        if (ModelState.IsValid)
        {
            maquinaBanco.Fabricante = maquina.Fabricante;
            maquinaBanco.Modelo = maquina.Modelo;
            maquinaBanco.Potencia = maquina.Potencia;
            maquinaBanco.Tensao = maquina.Tensao;
            maquinaBanco.Corrente = maquina.Corrente;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Máquina atualizada com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        return View(maquina);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == id &&
                m.UsuarioId == usuarioId);

        if (maquina == null)
            return NotFound();

        return View(maquina);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
            return NotFound();

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m =>
                m.IdMaquina == id &&
                m.UsuarioId == usuarioId);

        if (maquina != null)
        {
            _context.Maquinas.Remove(maquina);
            await _context.SaveChangesAsync();

            TempData["Excluido"] = "Máquina excluída com sucesso.";
        }

        return RedirectToAction(nameof(Index));
    }
}