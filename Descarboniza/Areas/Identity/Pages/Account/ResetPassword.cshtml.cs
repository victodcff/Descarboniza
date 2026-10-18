#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Descarboniza.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Descarboniza.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private const string MensagemLinkInvalido =
            "Este link é inválido ou já expirou. Peça um novo link em \"Esqueci minha senha\".";

        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Informe o seu e-mail.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Informe a nova senha.")]
            [StringLength(100, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required(ErrorMessage = "Confirme a nova senha.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "A senha e a confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return RedirectToPage("./ForgotPassword");
            }

            try
            {
                Input = new InputModel
                {
                    Email = email,
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
            }
            catch (FormatException)
            {
                // Link copiado incompleto ou alterado
                return RedirectToPage("./ForgotPassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var usuario = await _userManager.FindByEmailAsync(Input.Email.Trim());

            // Mesma resposta para e-mail inexistente e para a conta do administrador.
            if (usuario == null ||
                await _userManager.IsInRoleAsync(usuario, AppRoles.Admin))
            {
                ModelState.AddModelError(string.Empty, MensagemLinkInvalido);
                return Page();
            }

            var resultado = await _userManager.ResetPasswordAsync(usuario, Input.Code, Input.Password);

            if (resultado.Succeeded)
            {
                TempData["Sucesso"] = "Senha redefinida com sucesso. Entre com a nova senha.";
                return RedirectToPage("./Login");
            }

            foreach (var erro in resultado.Errors)
            {
                if (erro.Code == "InvalidToken")
                {
                    ModelState.AddModelError(string.Empty, MensagemLinkInvalido);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }
            }

            return Page();
        }
    }
}
