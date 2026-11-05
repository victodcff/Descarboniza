using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Descarboniza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.RegularExpressions;

[Authorize]
public class ChatController : Controller
{
    private const int MaxMensagens = 12;
    private const int MaxTexto = 800;
    private const int MaxMaquinas = 25;
    private const int LimitePorHora = 40;
    private static readonly TimeSpan IntervaloMinimo = TimeSpan.FromSeconds(3);

    private static readonly Regex Controle =
        new(@"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]", RegexOptions.Compiled);

    private readonly DescarbonizaContext _context;
    private readonly AnaliseConsumoService _analiseService;
    private readonly EmissaoCarbonoService _emissaoService;
    private readonly IaMaquinaService _ia;
    private readonly IMemoryCache _cache;

    public ChatController(
        DescarbonizaContext context,
        AnaliseConsumoService analiseService,
        EmissaoCarbonoService emissaoService,
        IaMaquinaService ia,
        IMemoryCache cache)
    {
        _context = context;
        _analiseService = analiseService;
        _emissaoService = emissaoService;
        _ia = ia;
        _cache = cache;
    }

    // POST: Chat/Enviar  (chamado por JavaScript pelo botão flutuante)
    // O site monta os dados do usuário e a IA só conversa em cima deles:
    // os números nunca vêm do navegador.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar([FromBody] ChatPedido? pedido, CancellationToken ct)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var mensagens = LimparMensagens(pedido?.Mensagens);

        if (mensagens.Count == 0 || mensagens[^1].Papel != "usuario")
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { sucesso = false, erro = "Escreva uma mensagem para conversar." });
        }

        // Proteção da cota da IA: 1 mensagem a cada 3 segundos e até 40 por hora.
        var chaveEspera = $"chat-espera|{usuarioId}";

        if (_cache.TryGetValue(chaveEspera, out _))
            return Limite("Calma, envie uma mensagem de cada vez.");

        _cache.Set(chaveEspera, true, IntervaloMinimo);

        var contador = _cache.GetOrCreate($"chat-hora|{usuarioId}", entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return new int[1];
        })!;

        if (Interlocked.Increment(ref contador[0]) > LimitePorHora)
            return Limite("Você atingiu o limite de mensagens por hora. Tente novamente mais tarde.");

        var contexto = await MontarContextoAsync(usuarioId, pedido?.IdConsumo, ct);

        var resposta = await _ia.ConversarAsync(
            new ChatIaRequest { Mensagens = mensagens, Contexto = contexto }, ct);

        return Json(new
        {
            sucesso = resposta.Sucesso,
            erro = resposta.Erro,
            resposta = resposta.Resposta
        });
    }

    private IActionResult Limite(string mensagem)
    {
        Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return Json(new { sucesso = false, erro = mensagem });
    }

    // Aceita só "usuario" e "ia", corta o tamanho e mantém as últimas mensagens.
    // A conversa enviada à IA sempre começa por uma mensagem do usuário.
    private static List<ChatMensagem> LimparMensagens(List<ChatMensagem>? recebidas)
    {
        var lista = new List<ChatMensagem>();

        foreach (var m in recebidas ?? new List<ChatMensagem>())
        {
            var papel = (m.Papel ?? string.Empty).Trim().ToLowerInvariant();
            var texto = Limpar(m.Texto, MaxTexto);

            if ((papel != "usuario" && papel != "ia") || texto.Length == 0)
                continue;

            lista.Add(new ChatMensagem { Papel = papel, Texto = texto });
        }

        if (lista.Count > MaxMensagens)
            lista = lista.GetRange(lista.Count - MaxMensagens, MaxMensagens);

        while (lista.Count > 0 && lista[0].Papel != "usuario")
            lista.RemoveAt(0);

        return lista;
    }

    private static string Limpar(string? texto, int limite)
    {
        var t = Controle.Replace(texto ?? string.Empty, " ").Trim();
        return t.Length > limite ? t[..limite] : t;
    }

    private object ResumirMaquina(Consumo c, double totalMensalKwh)
    {
        return new
        {
            fabricante = Limpar(c.Maquina?.Fabricante, 60),
            modelo = Limpar(c.Maquina?.Modelo, 60),
            potenciaKw = Math.Round(c.Maquina?.Potencia ?? 0, 3),
            tensaoV = c.Maquina?.Tensao,
            correnteA = c.Maquina?.Corrente,
            horasUsoDia = c.HorasUsoDia,
            diasUsoMes = c.DiasUsoMes,
            tarifaReaisPorKwh = Math.Round(c.TarifaKwh, 4),
            consumoMensalKwh = Math.Round(c.ConsumoKwhMes, 2),
            consumoAnualKwh = Math.Round(_analiseService.CalcularConsumoAnual(c), 2),
            custoMensalReais = Math.Round(_analiseService.CalcularCustoMensal(c), 2),
            custoAnualReais = Math.Round(_analiseService.CalcularCustoAnual(c), 2),
            emissaoMensalKgCo2 = Math.Round(_emissaoService.CalcularEmissao(c.ConsumoKwhMes), 2),
            participacaoPercentual = totalMensalKwh > 0
                ? Math.Round(c.ConsumoKwhMes / totalMensalKwh * 100, 1)
                : 0
        };
    }

    private async Task<object> MontarContextoAsync(
        string usuarioId,
        int? idConsumoAberto,
        CancellationToken ct)
    {
        var consumos = await _context.Consumos
            .Include(c => c.Maquina)
            .Where(c => c.Maquina != null && c.Maquina.UsuarioId == usuarioId)
            .ToListAsync(ct);

        var maquinas = await _context.Maquinas
            .Where(m => m.UsuarioId == usuarioId)
            .ToListAsync(ct);

        var total = consumos.Sum(c => c.ConsumoKwhMes);

        var comConsumo = consumos
            .OrderByDescending(c => c.ConsumoKwhMes)
            .Take(MaxMaquinas)
            .Select(c => ResumirMaquina(c, total))
            .ToList();

        var idsComConsumo = consumos.Select(c => c.IdMaquina).ToHashSet();

        var semConsumo = maquinas
            .Where(m => !idsComConsumo.Contains(m.IdMaquina))
            .Take(MaxMaquinas)
            .Select(m => new
            {
                fabricante = Limpar(m.Fabricante, 60),
                modelo = Limpar(m.Modelo, 60),
                potenciaKw = Math.Round(m.Potencia, 3)
            })
            .ToList();

        // Máquina escolhida pelo usuário (seletor da página do assistente ou tela de Análise).
        var emFoco = idConsumoAberto == null
            ? null
            : consumos.FirstOrDefault(c => c.IdConsumo == idConsumoAberto);

        return new
        {
            resumo = new
            {
                totalMaquinasCadastradas = maquinas.Count,
                maquinasComConsumo = consumos.Count,
                consumoTotalMensalKwh = Math.Round(total, 2),
                custoTotalMensalReais = Math.Round(consumos.Sum(c => _analiseService.CalcularCustoMensal(c)), 2),
                emissaoTotalMensalKgCo2 = Math.Round(_emissaoService.CalcularEmissao(total), 2),
                fatorEmissaoKgCo2PorKwh = EmissaoCarbonoService.FatorEmissaoPadrao,
                observacao = consumos.Count > MaxMaquinas
                    ? $"Lista limitada às {MaxMaquinas} máquinas de maior consumo."
                    : null
            },
            maquinasComConsumo = comConsumo,
            maquinasSemConsumoRegistrado = semConsumo,
            maquinaEmFoco = emFoco == null
                ? null
                : $"{Limpar(emFoco.Maquina?.Fabricante, 60)} {Limpar(emFoco.Maquina?.Modelo, 60)}".Trim(),
            detalheDaMaquinaEmFoco = emFoco == null ? null : ResumirMaquina(emFoco, total)
        };
    }
}
