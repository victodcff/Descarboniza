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

                    // A logo vem do próprio site (precisa ser um endereço público; no Render é).
                    var enderecoBase = !string.IsNullOrWhiteSpace(urlPublica)
                        ? urlPublica
                        : $"{Request.Scheme}://{Request.Host}";

                    var html = MontarEmail(linkSeguro, HtmlEncoder.Default.Encode(enderecoBase + "/img/logo.png"));

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

        private static string MontarEmail(string link, string urlLogo)
        {
            const string fonte = "font-family:Arial,Helvetica,sans-serif;";

            return
                "<!DOCTYPE html><html lang='pt-BR'><head><meta charset='utf-8'>" +
                "<meta name='viewport' content='width=device-width,initial-scale=1'>" +
                "<meta name='color-scheme' content='light'></head>" +
                "<body style='margin:0;padding:0;background:#f3f6f4;'>" +

                // Texto de pré-visualização (aparece ao lado do assunto na caixa de entrada)
                "<div style='display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;'>" +
                "Use o botão para criar uma nova senha. O link vale por 1 hora." +
                "</div>" +

                "<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f3f6f4;padding:32px 12px;'>" +
                "<tr><td align='center'>" +

                "<table role='presentation' width='100%' cellpadding='0' cellspacing='0' " +
                "style='max-width:520px;background:#ffffff;border:1px solid #dfe7e1;border-radius:16px;overflow:hidden;'>" +

                // Faixa verde no topo
                "<tr><td style='height:6px;background:#4d9f20;font-size:0;line-height:0;'>&nbsp;</td></tr>" +

                // Logo e nome
                "<tr><td align='center' style='padding:32px 32px 8px;'>" +
                "<img src='" + urlLogo + "' width='84' height='84' alt='Descarboniza' style='display:block;border:0;outline:none;'>" +
                "<div style='margin-top:12px;" + fonte + "font-size:20px;font-weight:bold;letter-spacing:1px;color:#1f2a22;'>" +
                "<span style='color:#4d9f20;'>DES</span>CARBONIZA</div>" +
                "</td></tr>" +

                // Título e texto
                "<tr><td style='padding:20px 32px 0;" + fonte + "text-align:center;'>" +
                "<h1 style='margin:0 0 12px;font-size:22px;color:#1f2a22;'>Redefina sua senha</h1>" +
                "<p style='margin:0 0 26px;font-size:15px;line-height:1.6;color:#44514a;'>" +
                "Recebemos um pedido para redefinir a senha da sua conta. " +
                "Clique no botão abaixo para escolher uma nova senha." +
                "</p></td></tr>" +

                // Botão
                "<tr><td align='center' style='padding:0 32px;'>" +
                "<a href='" + link + "' style='display:inline-block;padding:14px 34px;background:#4d9f20;color:#ffffff;" +
                "text-decoration:none;border-radius:10px;" + fonte + "font-size:16px;font-weight:bold;'>" +
                "Redefinir minha senha</a></td></tr>" +

                // Aviso de validade
                "<tr><td style='padding:22px 32px 0;" + fonte + "font-size:13px;line-height:1.6;color:#5b6b60;text-align:center;'>" +
                "O link vale por <b>1 hora</b> e só pode ser usado uma vez." +
                "</td></tr>" +

                "<tr><td style='padding:22px 32px 0;'><div style='border-top:1px solid #e6ece8;font-size:0;line-height:0;'>&nbsp;</div></td></tr>" +

                // Rodapé do cartão
                "<tr><td style='padding:16px 32px 28px;" + fonte + "font-size:12px;line-height:1.7;color:#7a887f;text-align:center;'>" +
                "Se o botão não funcionar, copie e cole este endereço no navegador:<br>" +
                "<a href='" + link + "' style='color:#3f8a17;word-break:break-all;'>" + link + "</a><br><br>" +
                "Se você não fez esse pedido, pode ignorar este e-mail. Sua senha continua a mesma." +
                "</td></tr>" +

                "</table>" +

                // Rodapé fora do cartão
                "<p style='margin:18px 0 0;" + fonte + "font-size:12px;color:#8a978f;text-align:center;'>" +
                "Descarboniza &middot; gestão de consumo de energia e emissões de CO&#8322;</p>" +

                "</td></tr></table></body></html>";
        }
    }
}
