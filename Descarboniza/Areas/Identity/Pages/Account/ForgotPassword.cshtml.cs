#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Descarboniza.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Descarboniza.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            IConfiguration config,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _config = config;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Informe o seu e-mail.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();
            var usuario = await _userManager.FindByEmailAsync(email);

            // A conta do administrador não usa este fluxo (a senha dela vem da configuração).
            // Para quem pede, a resposta é sempre a mesma: assim ninguém descobre
            // quais e-mails têm conta.
            if (usuario != null &&
                !await _userManager.IsInRoleAsync(usuario, AppRoles.Admin))
            {
                try
                {
                    var codigo = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                    codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(codigo));

                    var caminho = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = codigo, email = usuario.Email });

                    // Se App:UrlPublica estiver definida, o link usa esse endereço
                    // (protege contra cabeçalho Host falso). Senão, usa o endereço do pedido.
                    var urlPublica = _config["App:UrlPublica"]?.Trim().TrimEnd('/');

                    var link = !string.IsNullOrWhiteSpace(urlPublica)
                        ? urlPublica + caminho
                        : $"{Request.Scheme}://{Request.Host}{caminho}";

                    var linkSeguro = HtmlEncoder.Default.Encode(link);

                    var html =
                        "<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#1f2a22;line-height:1.5\">" +
                        "<h2 style=\"color:#3f8a17;margin-bottom:4px\">Descarboniza</h2>" +
                        "<p>Recebemos um pedido para redefinir a senha da sua conta.</p>" +
                        "<p><a href=\"" + linkSeguro + "\" style=\"display:inline-block;padding:12px 22px;background:#4d9f20;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:bold\">Redefinir minha senha</a></p>" +
                        "<p style=\"font-size:13px;color:#5b6b60\">O link vale por 1 hora e só pode ser usado uma vez. " +
                        "Se você não fez esse pedido, ignore este e-mail: sua senha continua a mesma.</p>" +
                        "</div>";

                    await _emailSender.SendEmailAsync(
                        usuario.Email,
                        "Redefinir sua senha - Descarboniza",
                        html);
                }
                catch (Exception ex)
                {
                    // O usuário não vê o erro (para não revelar nada); fica registrado no log.
                    _logger.LogError(ex, "Falha ao enviar o e-mail de recuperação de senha.");
                }
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
