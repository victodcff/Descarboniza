using System.Security.Claims;
using Descarboniza.Security;
using Microsoft.AspNetCore.Identity;

namespace Descarboniza.Services
{
    /// <summary>
    /// Garante, ao iniciar o sistema, que existe o papel "Admin" e que
    /// exatamente UM usuário o possui (o definido em Admin:Email / Admin:Senha).
    /// Os dois valores NÃO ficam no código: use User Secrets (desenvolvimento)
    /// ou variáveis de ambiente Admin__Email / Admin__Senha (hospedagem).
    /// </summary>
    public static class AdminSeedService
    {
        public static async Task GarantirAdminAsync(IServiceProvider servicos)
        {
            var log = servicos.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AdminSeed");

            try
            {
                var config = servicos.GetRequiredService<IConfiguration>();
                var roleManager = servicos.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = servicos.GetRequiredService<UserManager<IdentityUser>>();

                if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
                {
                    await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
                }

                var email = config["Admin:Email"]?.Trim();
                var senha = config["Admin:Senha"];

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
                {
                    log.LogWarning(
                        "Admin:Email e Admin:Senha não configurados. Nenhum administrador foi criado.");
                    return;
                }

                var admin = await userManager.FindByEmailAsync(email);

                if (admin == null)
                {
                    admin = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var criado = await userManager.CreateAsync(admin, senha);

                    if (!criado.Succeeded)
                    {
                        log.LogError("Não foi possível criar o administrador: {Erros}",
                            string.Join("; ", criado.Errors.Select(e => e.Description)));
                        return;
                    }

                    await userManager.AddClaimAsync(admin, new Claim("Nome", "Administrador"));
                    await userManager.AddClaimAsync(admin, new Claim("Sobrenome", ""));
                }

                if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                }

                // Só pode existir um administrador: tira o papel de qualquer outro.
                var comPapel = await userManager.GetUsersInRoleAsync(AppRoles.Admin);

                foreach (var outro in comPapel.Where(u => u.Id != admin.Id))
                {
                    await userManager.RemoveFromRoleAsync(outro, AppRoles.Admin);
                    log.LogWarning("Papel Admin removido de {Email}: só há um administrador.", outro.Email);
                }
            }
            catch (Exception ex)
            {
                // Não derruba o sistema se o banco estiver fora do ar na inicialização.
                log.LogError(ex, "Falha ao garantir o administrador.");
            }
        }
    }
}
