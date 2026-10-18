using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Descarboniza.Services
{
    /// <summary>
    /// Envia e-mails pela API do Brevo (HTTPS). Usado na recuperação de senha.
    /// Configuração (User Secrets ou variáveis de ambiente):
    ///   Brevo:ApiKey, Brevo:RemetenteEmail, Brevo:RemetenteNome
    /// </summary>
    public class BrevoEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(HttpClient http, IConfiguration config, ILogger<BrevoEmailSender> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var chave = _config["Brevo:ApiKey"];
            var remetenteEmail = _config["Brevo:RemetenteEmail"];
            var remetenteNome = _config["Brevo:RemetenteNome"];

            if (string.IsNullOrWhiteSpace(remetenteNome))
                remetenteNome = "Descarboniza";

            if (string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(remetenteEmail))
                throw new InvalidOperationException(
                    "Envio de e-mail não configurado (Brevo:ApiKey e Brevo:RemetenteEmail).");

            using var pedido = new HttpRequestMessage(HttpMethod.Post, "smtp/email");

            pedido.Headers.Add("api-key", chave);
            pedido.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            pedido.Content = JsonContent.Create(new
            {
                sender = new { name = remetenteNome, email = remetenteEmail },
                to = new[] { new { email } },
                subject,
                htmlContent = htmlMessage
            });

            using var resposta = await _http.SendAsync(pedido);

            if (!resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadAsStringAsync();

                _logger.LogError("O Brevo recusou o envio ({Status}): {Corpo}",
                    (int)resposta.StatusCode, corpo);

                throw new InvalidOperationException("Não foi possível enviar o e-mail.");
            }
        }
    }
}
