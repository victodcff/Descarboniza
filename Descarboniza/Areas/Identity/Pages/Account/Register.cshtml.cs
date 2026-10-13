#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Descarboniza.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IConfiguration _config;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            IConfiguration config)
        {
            _userManager = userManager;
            _userStore = userStore;
            _config = config;
            _emailStore = GetEmailStore();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Informe o seu nome.")]
            [StringLength(100, ErrorMessage = "O nome pode ter até {1} caracteres.")]
            public string Nome { get; set; }

            [Required(ErrorMessage = "Informe o seu sobrenome.")]
            [StringLength(100, ErrorMessage = "O sobrenome pode ter até {1} caracteres.")]
            public string Sobrenome { get; set; }

            [Required(ErrorMessage = "Informe o seu e-mail.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Informe uma senha.")]
            [StringLength(100, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required(ErrorMessage = "Confirme a sua senha.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "A senha e a confirmação não coincidem.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();

            // O e-mail do administrador é reservado: ninguém pode se cadastrar com ele.
            var emailAdmin = _config["Admin:Email"]?.Trim();

            if (!string.IsNullOrWhiteSpace(emailAdmin) &&
                string.Equals(email, emailAdmin, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Este e-mail não está disponível.");
                return Page();
            }

            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Salva o nome e o sobrenome do usuário
                await _userManager.AddClaimAsync(user, new Claim("Nome", Input.Nome.Trim()));
                await _userManager.AddClaimAsync(user, new Claim("Sobrenome", Input.Sobrenome.Trim()));

                TempData["Sucesso"] = "Conta criada com sucesso. Faça login para continuar.";

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar uma instância de '{nameof(IdentityUser)}'.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "O Identity precisa oferecer suporte a e-mail.");
            }

            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}