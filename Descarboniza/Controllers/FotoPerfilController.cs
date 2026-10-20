using Descarboniza.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Descarboniza.Controllers
{
    [Authorize]
    public class FotoPerfilController : Controller
    {
        // O navegador já reduz a foto para 256x256 (cerca de 20 a 40 KB).
        // Este limite só barra arquivos que fujam disso.
        private const int TamanhoMaximoBytes = 200 * 1024;

        // Claim que guarda a "versão" da foto. Assim a barra lateral sabe se
        // existe foto sem consultar o banco a cada página, e o navegador
        // busca a imagem de novo quando ela muda.
        public const string TipoClaim = "FotoVersao";

        private readonly FotoPerfilService _fotos;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public FotoPerfilController(
            FotoPerfilService fotos,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _fotos = fotos;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Cada usuário só enxerga a própria foto.
        [HttpGet("Perfil/Foto")]
        public async Task<IActionResult> Obter()
        {
            var usuarioId = _userManager.GetUserId(User);

            if (usuarioId == null)
                return NotFound();

            var dados = await _fotos.ObterAsync(usuarioId);

            if (dados == null)
                return NotFound();

            Response.Headers["Cache-Control"] = "private, max-age=86400";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(dados, "image/jpeg");
        }

        [HttpPost("Perfil/Foto")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> Enviar(IFormFile? arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { erro = "Escolha uma imagem." });

            if (arquivo.Length > TamanhoMaximoBytes)
                return BadRequest(new { erro = "A imagem ficou grande demais. Escolha outra." });

            byte[] dados;

            using (var memoria = new MemoryStream())
            {
                await arquivo.CopyToAsync(memoria);
                dados = memoria.ToArray();
            }

            if (!EhJpeg(dados))
                return BadRequest(new { erro = "Formato inválido. Use uma imagem JPG, PNG ou WEBP." });

            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return Unauthorized();

            await _fotos.SalvarAsync(usuario.Id, dados);

            await DefinirVersaoAsync(usuario);

            // Atualiza o cookie para a barra lateral já mostrar a foto nova
            await _signInManager.RefreshSignInAsync(usuario);

            TempData["Sucesso"] = "Foto de perfil atualizada.";

            return Json(new { ok = true });
        }

        [HttpPost("Perfil/Foto/Remover")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remover()
        {
            var usuario = await _userManager.GetUserAsync(User);

            if (usuario == null)
                return Unauthorized();

            await _fotos.RemoverAsync(usuario.Id);

            var claims = await _userManager.GetClaimsAsync(usuario);
            var existente = claims.FirstOrDefault(c => c.Type == TipoClaim);

            if (existente != null)
                await _userManager.RemoveClaimAsync(usuario, existente);

            await _signInManager.RefreshSignInAsync(usuario);

            TempData["Sucesso"] = "Foto de perfil removida.";

            return RedirectToPage("/Account/Manage/Index", new { area = "Identity" });
        }

        private async Task DefinirVersaoAsync(IdentityUser usuario)
        {
            var versao = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var claims = await _userManager.GetClaimsAsync(usuario);
            var existente = claims.FirstOrDefault(c => c.Type == TipoClaim);

            if (existente == null)
                await _userManager.AddClaimAsync(usuario, new Claim(TipoClaim, versao));
            else
                await _userManager.ReplaceClaimAsync(usuario, existente, new Claim(TipoClaim, versao));
        }

        // Todo JPEG começa com os bytes FF D8 FF. O navegador sempre converte
        // para JPEG antes de enviar, então qualquer outra coisa é recusada.
        private static bool EhJpeg(byte[] dados)
        {
            return dados.Length > 3
                && dados[0] == 0xFF
                && dados[1] == 0xD8
                && dados[2] == 0xFF;
        }
    }
}
