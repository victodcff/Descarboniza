namespace Descarboniza.ViewModels
{
    /// <summary>Uma mensagem da conversa. Papel: "usuario" ou "ia".</summary>
    public class ChatMensagem
    {
        public string Papel { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
    }

    /// <summary>O que o navegador envia ao site.</summary>
    public class ChatPedido
    {
        public List<ChatMensagem> Mensagens { get; set; } = new();

        /// <summary>Consumo aberto na tela (quando o usuário está numa Análise).</summary>
        public int? IdConsumo { get; set; }
    }

    /// <summary>O que o site envia à IA (nomes em camelCase no JSON).</summary>
    public class ChatIaRequest
    {
        public List<ChatMensagem> Mensagens { get; set; } = new();
        public object Contexto { get; set; } = new();
    }

    /// <summary>Resposta da IA (POST /chat).</summary>
    public class ChatIaResponse
    {
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public string? Resposta { get; set; }
    }
}
