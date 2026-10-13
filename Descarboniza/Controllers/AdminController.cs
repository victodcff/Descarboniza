using System.Security.Claims;
using Descarboniza.Areas.Identity.Data;
using Descarboniza.Security;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[SomenteAdmin]
public class AdminController : Controller
{
    private readonly DescarbonizaContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(
        DescarbonizaContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =====================================================
    //  APOIO
    // =====================================================

    private async Task<HashSet<string>> ObterIdsAdminAsync()
    {
        var ids = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where r.Name == AppRoles.Admin
            select ur.UserId).ToListAsync();

        return ids.ToHashSet();
    }

    private async Task<Dictionary<string, AdminUsuarioInfo>> CarregarUsuariosAsync()
    {
        var usuarios = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        var claims = await _context.UserClaims
            .AsNoTracking()
            .Where(c => c.ClaimType == "Nome" || c.ClaimType == "Sobrenome")
            .ToListAsync();

        return usuarios.ToDictionary(
            u => u.Id,
            u => new AdminUsuarioInfo
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                Nome = claims.FirstOrDefault(c => c.UserId == u.Id && c.ClaimType == "Nome")?.ClaimValue ?? string.Empty,
                Sobrenome = claims.FirstOrDefault(c => c.UserId == u.Id && c.ClaimType == "Sobrenome")?.ClaimValue ?? string.Empty
            });
    }

    private static (string Nome, string Email) Dono(
        Dictionary<string, AdminUsuarioInfo> usuarios, string? usuarioId)
    {
        if (usuarioId != null && usuarios.TryGetValue(usuarioId, out var u))
        {
            return (u.NomeCompleto, u.Email);
        }

        return ("—", string.Empty);
    }

    private async Task<List<AdminUsuarioInfo>> UsuariosComunsAsync(
        Dictionary<string, AdminUsuarioInfo> usuarios)
    {
        var admins = await ObterIdsAdminAsync();

        return usuarios.Values
            .Where(u => !admins.Contains(u.Id))
            .OrderBy(u => u.NomeCompleto)
            .ToList();
    }

    private async Task<string?> NomeDoDonoAsync(string? usuarioId)
    {
        if (usuarioId == null)
        {
            return null;
        }

        var usuarios = await CarregarUsuariosAsync();

        return Dono(usuarios, usuarioId).Nome;
    }

    // =====================================================
    //  PAINEL
    // =====================================================

    public async Task<IActionResult> Index()
    {
        var usuarios = await CarregarUsuariosAsync();
        var admins = await ObterIdsAdminAsync();

        var maquinasPorUsuario = await _context.Maquinas
            .AsNoTracking()
            .Where(m => m.UsuarioId != null)
            .GroupBy(m => m.UsuarioId!)
            .Select(g => new { UsuarioId = g.Key, Qtd = g.Count() })
            .ToListAsync();

        var painel = new AdminPainelViewModel
        {
            TotalUsuarios = usuarios.Keys.Count(id => !admins.Contains(id)),
            TotalMaquinas = await _context.Maquinas.CountAsync(),
            TotalConsumos = await _context.Consumos.CountAsync(),
            TotalEmissoes = await _context.EmissoesCarbono.CountAsync(),
            ConsumoTotalKwhMes = await _context.Consumos
                .SumAsync(c => (double?)c.ConsumoKwhMes) ?? 0,
            EmissaoTotalKg = await _context.EmissoesCarbono
                .SumAsync(e => (double?)e.EmissaoKgCo2) ?? 0,
            MaioresUsuarios = maquinasPorUsuario
                .Where(x => usuarios.ContainsKey(x.UsuarioId) && !admins.Contains(x.UsuarioId))
                .OrderByDescending(x => x.Qtd)
                .Take(5)
                .Select(x => new AdminUsuarioItem
                {
                    Usuario = usuarios[x.UsuarioId],
                    QtdMaquinas = x.Qtd
                })
                .ToList()
        };

        return View(painel);
    }

    // =====================================================
    //  USUÁRIOS
    // =====================================================

    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await CarregarUsuariosAsync();
        var admins = await ObterIdsAdminAsync();

        var contagem = await _context.Maquinas
            .AsNoTracking()
            .Where(m => m.UsuarioId != null)
            .GroupBy(m => m.UsuarioId!)
            .Select(g => new { UsuarioId = g.Key, Qtd = g.Count() })
            .ToDictionaryAsync(x => x.UsuarioId, x => x.Qtd);

        var itens = usuarios.Values
            .OrderBy(u => admins.Contains(u.Id) ? 0 : 1)
            .ThenBy(u => u.NomeCompleto)
            .Select(u => new AdminUsuarioItem
            {
                Usuario = u,
                EhAdmin = admins.Contains(u.Id),
                QtdMaquinas = contagem.TryGetValue(u.Id, out var q) ? q : 0
            })
            .ToList();

        return View(itens);
    }

    public async Task<IActionResult> EditarUsuario(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var usuarios = await CarregarUsuariosAsync();

        if (!usuarios.TryGetValue(id, out var info))
            return NotFound();

        if ((await ObterIdsAdminAsync()).Contains(id))
        {
            TempData["Erro"] = "Os dados do administrador são alterados em Configurações.";
            return RedirectToAction(nameof(Usuarios));
        }

        return View(new AdminUsuarioEditViewModel
        {
            Id = info.Id,
            Nome = info.Nome,
            Sobrenome = info.Sobrenome,
            Email = info.Email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(AdminUsuarioEditViewModel modelo)
    {
        var usuario = await _userManager.FindByIdAsync(modelo.Id);

        if (usuario == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(usuario, AppRoles.Admin))
        {
            TempData["Erro"] = "Os dados do administrador são alterados em Configurações.";
            return RedirectToAction(nameof(Usuarios));
        }

        if (!ModelState.IsValid)
            return View(modelo);

        var email = modelo.Email.Trim();

        if (!string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await _userManager.FindByEmailAsync(email);

            if (existente != null && existente.Id != usuario.Id)
            {
                ModelState.AddModelError(nameof(modelo.Email), "Já existe um usuário com este e-mail.");
                return View(modelo);
            }

            var r1 = await _userManager.SetUserNameAsync(usuario, email);
            var r2 = await _userManager.SetEmailAsync(usuario, email);

            if (!r1.Succeeded || !r2.Succeeded)
            {
                foreach (var erro in r1.Errors.Concat(r2.Errors))
                    ModelState.AddModelError(string.Empty, erro.Description);

                return View(modelo);
            }
        }

        var claims = await _userManager.GetClaimsAsync(usuario);

        foreach (var antiga in claims.Where(c => c.Type == "Nome" || c.Type == "Sobrenome"))
        {
            await _userManager.RemoveClaimAsync(usuario, antiga);
        }

        await _userManager.AddClaimAsync(usuario, new Claim("Nome", modelo.Nome.Trim()));
        await _userManager.AddClaimAsync(usuario, new Claim("Sobrenome", modelo.Sobrenome.Trim()));

        TempData["Sucesso"] = "Usuário atualizado com sucesso.";

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirUsuario(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var usuario = await _userManager.FindByIdAsync(id);

        if (usuario == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(usuario, AppRoles.Admin))
        {
            TempData["Erro"] = "O administrador não pode ser excluído.";
            return RedirectToAction(nameof(Usuarios));
        }

        await using var transacao = await _context.Database.BeginTransactionAsync();

        var idsMaquinas = _context.Maquinas
            .Where(m => m.UsuarioId == id)
            .Select(m => m.IdMaquina);

        await _context.EmissoesCarbono
            .Where(e => idsMaquinas.Contains(e.IdMaquina))
            .ExecuteDeleteAsync();

        await _context.Consumos
            .Where(c => idsMaquinas.Contains(c.IdMaquina))
            .ExecuteDeleteAsync();

        await _context.Maquinas
            .Where(m => m.UsuarioId == id)
            .ExecuteDeleteAsync();

        var resultado = await _userManager.DeleteAsync(usuario);

        if (!resultado.Succeeded)
        {
            await transacao.RollbackAsync();
            TempData["Erro"] = "Não foi possível excluir o usuário.";
            return RedirectToAction(nameof(Usuarios));
        }

        await transacao.CommitAsync();

        TempData["Excluido"] = "Usuário e todos os seus dados foram excluídos.";

        return RedirectToAction(nameof(Usuarios));
    }

    // =====================================================
    //  MÁQUINAS
    // =====================================================

    public async Task<IActionResult> Maquinas(string? usuario)
    {
        var usuarios = await CarregarUsuariosAsync();

        var consulta = _context.Maquinas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
            consulta = consulta.Where(m => m.UsuarioId == usuario);

        var maquinas = await consulta
            .OrderBy(m => m.Fabricante)
            .ThenBy(m => m.Modelo)
            .ToListAsync();

        return View(new AdminListaViewModel<AdminMaquinaItem>
        {
            UsuarioFiltro = usuario,
            Usuarios = await UsuariosComunsAsync(usuarios),
            Itens = maquinas.Select(m =>
            {
                var dono = Dono(usuarios, m.UsuarioId);
                return new AdminMaquinaItem
                {
                    Maquina = m,
                    Dono = dono.Nome,
                    DonoEmail = dono.Email
                };
            }).ToList()
        });
    }

    public async Task<IActionResult> EditMaquina(int? id)
    {
        if (id == null)
            return NotFound();

        var maquina = await _context.Maquinas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdMaquina == id);

        if (maquina == null)
            return NotFound();

        ViewBag.Dono = await NomeDoDonoAsync(maquina.UsuarioId);

        return View(maquina);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMaquina(
        int id,
        [Bind("IdMaquina,Fabricante,Modelo,Potencia,Tensao,Corrente")]
        Maquina maquina)
    {
        if (id != maquina.IdMaquina)
            return NotFound();

        var banco = await _context.Maquinas
            .FirstOrDefaultAsync(m => m.IdMaquina == id);

        if (banco == null)
            return NotFound();

        if (maquina.Potencia < 0 || maquina.Tensao < 0 || maquina.Corrente < 0)
            ModelState.AddModelError(string.Empty, "Os valores não podem ser negativos.");

        if (ModelState.IsValid)
        {
            banco.Fabricante = maquina.Fabricante;
            banco.Modelo = maquina.Modelo;
            banco.Potencia = maquina.Potencia;
            banco.Tensao = maquina.Tensao;
            banco.Corrente = maquina.Corrente;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Máquina atualizada com sucesso.";

            return RedirectToAction(nameof(Maquinas));
        }

        maquina.DataCadastro = banco.DataCadastro;
        ViewBag.Dono = await NomeDoDonoAsync(banco.UsuarioId);

        return View(maquina);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirMaquina(int? id)
    {
        if (id == null)
            return NotFound();

        var maquina = await _context.Maquinas
            .FirstOrDefaultAsync(m => m.IdMaquina == id);

        if (maquina == null)
            return NotFound();

        await using var transacao = await _context.Database.BeginTransactionAsync();

        await _context.EmissoesCarbono
            .Where(e => e.IdMaquina == id)
            .ExecuteDeleteAsync();

        await _context.Consumos
            .Where(c => c.IdMaquina == id)
            .ExecuteDeleteAsync();

        _context.Maquinas.Remove(maquina);
        await _context.SaveChangesAsync();

        await transacao.CommitAsync();

        TempData["Excluido"] = "Máquina excluída com sucesso.";

        return RedirectToAction(nameof(Maquinas));
    }

    // =====================================================
    //  CONSUMOS
    // =====================================================

    public async Task<IActionResult> Consumos(string? usuario)
    {
        var usuarios = await CarregarUsuariosAsync();

        var consulta = _context.Consumos
            .AsNoTracking()
            .Include(c => c.Maquina)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
            consulta = consulta.Where(c => c.Maquina != null && c.Maquina.UsuarioId == usuario);

        var consumos = await consulta
            .OrderBy(c => c.Maquina!.Fabricante)
            .ThenBy(c => c.Maquina!.Modelo)
            .ToListAsync();

        return View(new AdminListaViewModel<AdminConsumoItem>
        {
            UsuarioFiltro = usuario,
            Usuarios = await UsuariosComunsAsync(usuarios),
            Itens = consumos.Select(c =>
            {
                var dono = Dono(usuarios, c.Maquina?.UsuarioId);
                return new AdminConsumoItem
                {
                    Consumo = c,
                    Dono = dono.Nome,
                    DonoEmail = dono.Email
                };
            }).ToList()
        });
    }

    public async Task<IActionResult> EditConsumo(int? id)
    {
        if (id == null)
            return NotFound();

        var consumo = await _context.Consumos
            .AsNoTracking()
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c => c.IdConsumo == id);

        if (consumo == null)
            return NotFound();

        ViewBag.Dono = await NomeDoDonoAsync(consumo.Maquina?.UsuarioId);

        return View(consumo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditConsumo(
        int id,
        [Bind("IdConsumo,HorasUsoDia,DiasUsoMes,ConsumoKwhMes,TarifaKwh")]
        Consumo consumo)
    {
        if (id != consumo.IdConsumo)
            return NotFound();

        var banco = await _context.Consumos
            .Include(c => c.Maquina)
            .FirstOrDefaultAsync(c => c.IdConsumo == id);

        if (banco == null)
            return NotFound();

        if (consumo.HorasUsoDia < 0 || consumo.DiasUsoMes < 0 ||
            consumo.ConsumoKwhMes < 0 || consumo.TarifaKwh < 0)
            ModelState.AddModelError(string.Empty, "Os valores não podem ser negativos.");

        if (consumo.HorasUsoDia > 24)
            ModelState.AddModelError(nameof(consumo.HorasUsoDia), "Um dia tem no máximo 24 horas.");

        if (consumo.DiasUsoMes > 31)
            ModelState.AddModelError(nameof(consumo.DiasUsoMes), "Um mês tem no máximo 31 dias.");

        if (ModelState.IsValid)
        {
            banco.HorasUsoDia = consumo.HorasUsoDia;
            banco.DiasUsoMes = consumo.DiasUsoMes;
            banco.ConsumoKwhMes = consumo.ConsumoKwhMes;
            banco.TarifaKwh = consumo.TarifaKwh;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Consumo atualizado com sucesso.";

            return RedirectToAction(nameof(Consumos));
        }

        consumo.IdMaquina = banco.IdMaquina;
        consumo.Maquina = banco.Maquina;
        consumo.DataRegistro = banco.DataRegistro;
        ViewBag.Dono = await NomeDoDonoAsync(banco.Maquina?.UsuarioId);

        return View(consumo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirConsumo(int? id)
    {
        if (id == null)
            return NotFound();

        var consumo = await _context.Consumos
            .FirstOrDefaultAsync(c => c.IdConsumo == id);

        if (consumo == null)
            return NotFound();

        _context.Consumos.Remove(consumo);
        await _context.SaveChangesAsync();

        TempData["Excluido"] = "Consumo excluído com sucesso.";

        return RedirectToAction(nameof(Consumos));
    }

    // =====================================================
    //  EMISSÕES
    // =====================================================

    public async Task<IActionResult> Emissoes(string? usuario)
    {
        var usuarios = await CarregarUsuariosAsync();

        var consulta = _context.EmissoesCarbono
            .AsNoTracking()
            .Include(e => e.Maquina)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
            consulta = consulta.Where(e => e.Maquina != null && e.Maquina.UsuarioId == usuario);

        var emissoes = await consulta
            .OrderBy(e => e.Maquina!.Fabricante)
            .ThenBy(e => e.Maquina!.Modelo)
            .ToListAsync();

        return View(new AdminListaViewModel<AdminEmissaoItem>
        {
            UsuarioFiltro = usuario,
            Usuarios = await UsuariosComunsAsync(usuarios),
            Itens = emissoes.Select(e =>
            {
                var dono = Dono(usuarios, e.Maquina?.UsuarioId);
                return new AdminEmissaoItem
                {
                    Emissao = e,
                    Dono = dono.Nome,
                    DonoEmail = dono.Email
                };
            }).ToList()
        });
    }

    public async Task<IActionResult> EditEmissao(int? id)
    {
        if (id == null)
            return NotFound();

        var emissao = await _context.EmissoesCarbono
            .AsNoTracking()
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e => e.IdEmissao == id);

        if (emissao == null)
            return NotFound();

        ViewBag.Dono = await NomeDoDonoAsync(emissao.Maquina?.UsuarioId);

        return View(emissao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmissao(
        int id,
        [Bind("IdEmissao,ConsumoKwh,FatorEmissao")]
        EmissaoCarbono emissao)
    {
        if (id != emissao.IdEmissao)
            return NotFound();

        var banco = await _context.EmissoesCarbono
            .Include(e => e.Maquina)
            .FirstOrDefaultAsync(e => e.IdEmissao == id);

        if (banco == null)
            return NotFound();

        if (emissao.ConsumoKwh < 0 || emissao.FatorEmissao < 0)
            ModelState.AddModelError(string.Empty, "Os valores não podem ser negativos.");

        if (ModelState.IsValid)
        {
            banco.ConsumoKwh = emissao.ConsumoKwh;
            banco.FatorEmissao = emissao.FatorEmissao;
            banco.EmissaoKgCo2 = emissao.ConsumoKwh * emissao.FatorEmissao;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Emissão atualizada com sucesso.";

            return RedirectToAction(nameof(Emissoes));
        }

        emissao.IdMaquina = banco.IdMaquina;
        emissao.Maquina = banco.Maquina;
        emissao.DataRegistro = banco.DataRegistro;
        emissao.EmissaoKgCo2 = banco.EmissaoKgCo2;
        ViewBag.Dono = await NomeDoDonoAsync(banco.Maquina?.UsuarioId);

        return View(emissao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEmissao(int? id)
    {
        if (id == null)
            return NotFound();

        var emissao = await _context.EmissoesCarbono
            .FirstOrDefaultAsync(e => e.IdEmissao == id);

        if (emissao == null)
            return NotFound();

        _context.EmissoesCarbono.Remove(emissao);
        await _context.SaveChangesAsync();

        TempData["Excluido"] = "Emissão excluída com sucesso.";

        return RedirectToAction(nameof(Emissoes));
    }
}
