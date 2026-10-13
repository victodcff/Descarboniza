using Descarboniza.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class IaController : Controller
{
    private readonly IaMaquinaService _ia;

    public IaController(IaMaquinaService ia)
    {
        _ia = ia;
    }

    // Chamada em segundo plano pela tela "Nova máquina" para acordar a IA
    // (no plano gratuito do Render ela dorme após 15 minutos sem uso).
    [HttpGet]
    public async Task<IActionResult> Aquecer(CancellationToken ct)
    {
        await _ia.AquecerAsync(ct);

        return NoContent();
    }
}
