#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Descarboniza.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public EmailModel(
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
            [Required(ErrorMessage = "Informe o novo e-mail.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            public string NovoEmail { get; set; }

            [Required(ErrorMessage = "Informe a sua senha atual.")]
            [DataType(DataType.Password)]
            public string SenhaAtual { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            Email = await _userManager.GetEmailAsync(user);
            Input = new InputModel();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            var emailAtual = await _userManager.GetEmailAsync(user);
            Email = emailAtual;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var novoEmail = Input.NovoEmail.Trim();

            if (string.Equals(novoEmail, emailAtual, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.NovoEmail", "Informe um e-mail diferente do atual.");
                return Page();
            }

            if (!await _userManager.CheckPasswordAsync(user, Input.SenhaAtual))
            {
                ModelState.AddModelError("Input.SenhaAtual", "Senha incorreta.");
                return Page();
            }

            // O e-mail não pode pertencer a outra conta
            var existente = await _userManager.FindByEmailAsync(novoEmail)
                ?? await _userManager.FindByNameAsync(novoEmail);

            if (existente != null && existente.Id != user.Id)
            {
                ModelState.AddModelError("Input.NovoEmail", "Este e-mail já está em uso por outra conta.");
                return Page();
            }

            // O login usa o e-mail como nome de usuário: os dois mudam juntos.
            // O Id do usuário não muda, então máquinas, consumos e emissões continuam dele.
            user.Email = novoEmail;
            user.UserName = novoEmail;
            user.EmailConfirmed = true;

            var resultado = await _userManager.UpdateAsync(user);

            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }

                return Page();
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            TempData["Sucesso"] = "E-mail alterado com sucesso. Use o novo e-mail para entrar.";

            return RedirectToPage();
        }
    }
}