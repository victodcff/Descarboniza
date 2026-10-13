#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Descarboniza.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Descarboniza.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly DescarbonizaContext _context;
        private readonly ILogger<DeletePersonalDataModel> _logger;

        public DeletePersonalDataModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            DescarbonizaContext context,
            ILogger<DeletePersonalDataModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Informe a sua senha para confirmar.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!await _userManager.CheckPasswordAsync(user, Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Senha incorreta.");
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);

            // Tudo ou nada: se algo falhar, nada é apagado
            await using var transacao = await _context.Database.BeginTransactionAsync();

            var maquinas = await _context.Maquinas
                .Where(m => m.UsuarioId == userId)
                .ToListAsync();

            var ids = maquinas.Select(m => m.IdMaquina).ToList();

            _context.EmissoesCarbono.RemoveRange(
                _context.EmissoesCarbono.Where(e => ids.Contains(e.IdMaquina)));

            _context.Consumos.RemoveRange(
                _context.Consumos.Where(c => ids.Contains(c.IdMaquina)));

            _context.Maquinas.RemoveRange(maquinas);

            await _context.SaveChangesAsync();

            var resultado = await _userManager.DeleteAsync(user);

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível excluir a conta. Tente novamente.");
                return Page();
            }

            await transacao.CommitAsync();

            await _signInManager.SignOutAsync();

            _logger.LogInformation("O usuário '{UserId}' excluiu a própria conta.", userId);

            TempData["Excluido"] = "Sua conta e todos os seus dados foram excluídos.";

            return Redirect("~/");
        }
    }
}