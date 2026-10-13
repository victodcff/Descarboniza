using System.Net;
using System.Text.Json;
using Descarboniza.ViewModels;

namespace Descarboniza.Services
{
    public class IaMaquinaService
    {
        // A IA hospedada no plano gratuito dorme e leva cerca de 1 minuto para acordar.
        // Enquanto isso a conexão falha ou a resposta não é JSON: tentamos de novo.
        private const int Tentativas = 8;
        private static readonly TimeSpan EsperaEntreTentativas = TimeSpan.FromSeconds(5);

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ILogger<IaMaquinaService> _logger;

        public IaMaquinaService(HttpClient http, ILogger<IaMaquinaService> logger)
        {
            _http = http;
            _logger = logger;
        }

        /// <summary>Acorda a IA sem analisar nada. Nunca lança erro.</summary>
        public async Task AquecerAsync(CancellationToken ct = default)
        {
            try
            {
                using var resposta = await _http.GetAsync("health", ct);
                _logger.LogInformation("IA respondeu ao aquecimento: {Status}.", (int)resposta.StatusCode);
            }
            catch (OperationCanceledException)
            {
                // Usuário saiu da página: sem problema.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível aquecer a IA.");
            }
        }

        public async Task<AnaliseMaquinaResponse> AnalisarAsync(
            IFormFile imagem,
            CancellationToken ct = default)
        {
            try
            {
                byte[] bytes;

                using (var memoria = new MemoryStream())
                {
                    await imagem.CopyToAsync(memoria, ct);
                    bytes = memoria.ToArray();
                }

                HttpResponseMessage resposta;

                for (var tentativa = 1; ; tentativa++)
                {
                    using var conteudo = new MultipartFormDataContent();
                    conteudo.Add(new ByteArrayContent(bytes), "imagem", "imagem");

                    try
                    {
                        resposta = await _http.PostAsync("analisar-maquina", conteudo, ct);
                    }
                    catch (HttpRequestException) when (tentativa < Tentativas)
                    {
                        _logger.LogWarning(
                            "Serviço de IA ainda não respondeu (tentativa {Tentativa}). Aguardando...",
                            tentativa);

                        await Task.Delay(EsperaEntreTentativas, ct);
                        continue;
                    }

                    // A nossa IA sempre responde JSON. Se vier outra coisa (página de
                    // "acordando" do Render, 502/503/504), ela ainda está iniciando.
                    if (!EhJson(resposta) && tentativa < Tentativas)
                    {
                        _logger.LogWarning(
                            "IA respondeu {Status} sem JSON (tentativa {Tentativa}). Aguardando...",
                            (int)resposta.StatusCode, tentativa);

                        resposta.Dispose();
                        await Task.Delay(EsperaEntreTentativas, ct);
                        continue;
                    }

                    break;
                }

                using (resposta)
                {
                    var corpo = await resposta.Content.ReadAsStringAsync(ct);

                    if (resposta.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.LogError("Serviço de IA recusou o token (401). Verifique IaService:Token.");
                        return Falha("Serviço de IA não configurado corretamente.");
                    }

                    var resultado = JsonSerializer.Deserialize<AnaliseMaquinaResponse>(corpo, JsonOptions);

                    if (resultado is null)
                        return Falha("Resposta inválida do serviço de IA.");

                    if (!resposta.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Serviço de IA respondeu {Status}.", (int)resposta.StatusCode);
                        resultado.Sucesso = false;

                        if (string.IsNullOrWhiteSpace(resultado.Erro))
                            resultado.Erro = "Não foi possível analisar a imagem.";
                    }

                    return resultado;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Timeout ao chamar o serviço de IA.");
                return Falha("A análise demorou demais. Tente novamente ou preencha manualmente.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Serviço de IA inacessível.");
                return Falha("Serviço de IA indisponível. Preencha os dados manualmente.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Resposta do serviço de IA fora do formato esperado.");
                return Falha("Resposta inválida do serviço de IA.");
            }
        }

        private static bool EhJson(HttpResponseMessage resposta) =>
            string.Equals(
                resposta.Content.Headers.ContentType?.MediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase);

        private static AnaliseMaquinaResponse Falha(string mensagem) =>
            new() { Sucesso = false, Erro = mensagem };
    }
}
