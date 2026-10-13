#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Descarboniza.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Informe o seu nome.")]
            [StringLength(100, ErrorMessage = "O nome pode ter até {1} caracteres.")]
            public string Nome { get; set; }

            [Required(ErrorMessage = "Informe o seu sobrenome.")]
            [StringLength(100, ErrorMessage = "O sobrenome pode ter até {1} caracteres.")]
            public string Sobrenome { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            Email = await _userManager.GetEmailAsync(user);

            var claims = await _userManager.GetClaimsAsync(user);

            Input = new InputModel
            {
                Nome = claims.FirstOrDefault(c => c.Type == "Nome")?.Value,
                Sobrenome = claims.FirstOrDefault(c => c.Type == "Sobrenome")?.Value
            };

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
                Email = await _userManager.GetEmailAsync(user);
                return Page();
            }

            await DefinirClaimAsync(user, "Nome", Input.Nome.Trim());
            await DefinirClaimAsync(user, "Sobrenome", Input.Sobrenome.Trim());

            // Atualiza o cookie para que o nome novo apareça na barra lateral
            await _signInManager.RefreshSignInAsync(user);

            TempData["Sucesso"] = "Perfil atualizado com sucesso.";

            return RedirectToPage();
        }

        private async Task DefinirClaimAsync(IdentityUser user, string tipo, string valor)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var existente = claims.FirstOrDefault(c => c.Type == tipo);

            if (existente == null)
            {
                await _userManager.AddClaimAsync(user, new Claim(tipo, valor));
            }
            else if (existente.Value != valor)
            {
                await _userManager.ReplaceClaimAsync(user, existente, new Claim(tipo, valor));
            }
        }
    }
}