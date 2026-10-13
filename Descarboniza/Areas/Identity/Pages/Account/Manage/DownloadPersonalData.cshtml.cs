#nullable disable

using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Descarboniza.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Descarboniza.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DescarbonizaContext _context;
        private readonly ILogger<DownloadPersonalDataModel> _logger;

        public DownloadPersonalDataModel(
            UserManager<IdentityUser> userManager,
            DescarbonizaContext context,
            ILogger<DownloadPersonalDataModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("O usuário '{UserId}' pediu os seus dados.", _userManager.GetUserId(User));

            var userId = await _userManager.GetUserIdAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var maquinas = await _context.Maquinas
                .Where(m => m.UsuarioId == userId)
                .Select(m => new
                {
                    m.IdMaquina,
                    m.Fabricante,
                    m.Modelo,
                    m.Potencia,
                    m.Tensao,
                    m.Corrente,
                    m.DataCadastro
                })
                .ToListAsync();

            var ids = maquinas.Select(m => m.IdMaquina).ToList();

            var consumos = await _context.Consumos
                .Where(c => ids.Contains(c.IdMaquina))
                .Select(c => new
                {
                    c.IdConsumo,
                    c.IdMaquina,
                    c.DataRegistro,
                    c.HorasUsoDia,
                    c.DiasUsoMes,
                    c.ConsumoKwhMes,
                    c.TarifaKwh
                })
                .ToListAsync();

            var emissoes = await _context.EmissoesCarbono
                .Where(e => ids.Contains(e.IdMaquina))
                .Select(e => new
                {
                    e.IdEmissao,
                    e.IdMaquina,
                    e.DataRegistro,
                    e.ConsumoKwh,
                    e.FatorEmissao,
                    e.EmissaoKgCo2
                })
                .ToListAsync();

            var dados = new
            {
                Conta = new
                {
                    Email = user.Email,
                    Nome = claims.FirstOrDefault(c => c.Type == "Nome")?.Value,
                    Sobrenome = claims.FirstOrDefault(c => c.Type == "Sobrenome")?.Value
                },
                Maquinas = maquinas,
                Consumos = consumos,
                Emissoes = emissoes
            };

            var opcoes = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(dados, opcoes);

            return File(bytes, "application/json", "meus-dados-descarboniza.json");
        }
    }
}