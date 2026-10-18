using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Descarboniza.Areas.Identity.Data;
using Descarboniza.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Hospedagem (Render): a porta vem da variável PORT.
var portaHospedagem = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(portaHospedagem))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portaHospedagem}");
}

var connectionString = builder.Configuration.GetConnectionString("DescarbonizaContextConnection") ?? throw new InvalidOperationException("Connection string 'DescarbonizaContextConnection' not found.");

builder.Services.AddDbContext<DescarbonizaContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<IdentityErrorDescriberPtBr>()
    .AddEntityFrameworkStores<DescarbonizaContext>();

// O link de "Esqueci minha senha" vale por 1 hora.
builder.Services.Configure<DataProtectionTokenProviderOptions>(opcoes =>
{
    opcoes.TokenLifespan = TimeSpan.FromHours(1);
});

// Envio de e-mail pela API do Brevo (recuperação de senha).
// Precisa vir depois do AddDefaultIdentity para substituir o envio padrão (que não faz nada).
builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Guarda as chaves de proteção (login e formulários) no banco. Sem isso,
// cada reinício do servidor deslogaria todo mundo.
builder.Services
    .AddDataProtection()
    .SetApplicationName("Descarboniza")
    .PersistKeysToDbContext<DescarbonizaContext>();

// O Render termina o HTTPS e repassa o pedido por HTTP: confia nos cabeçalhos dele.
builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
{
    opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opcoes.KnownNetworks.Clear();
    opcoes.KnownProxies.Clear();
});

builder.Services.AddScoped<AnaliseConsumoService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<EmissaoCarbonoService>();

var iaBaseUrl = builder.Configuration["IaService:BaseUrl"] ?? "http://127.0.0.1:8000/";
var iaToken = builder.Configuration["IaService:Token"];

builder.Services.AddHttpClient<IaMaquinaService>(client =>
{
    client.BaseAddress = new Uri(iaBaseUrl.TrimEnd('/') + "/");
    // Tempo total de cada chamada. A IA no plano gratuito pode levar ~1 min para acordar.
    client.Timeout = TimeSpan.FromSeconds(100);

    if (!string.IsNullOrWhiteSpace(iaToken))
        client.DefaultRequestHeaders.Add("X-Api-Key", iaToken);
});

// Iniciar o Python junto com o site só faz sentido no seu computador.
// Na hospedagem a IA é um serviço separado.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<IaProcessHostedService>();
}

var cultureInfo = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

// Páginas do Identity que o projeto não usa (dois fatores, login externo e
// fluxos que dependem de confirmação por e-mail). Quem abrir o endereço é redirecionado.
// "ForgotPassword" e "ResetPassword" ficam liberados: são a recuperação de senha por e-mail.
var paginasParaLogin = new[]
{
    "/Identity/Account/LoginWith2fa",
    "/Identity/Account/LoginWithRecoveryCode",
    "/Identity/Account/ResendEmailConfirmation",
    "/Identity/Account/ResetPasswordConfirmation",
    "/Identity/Account/ConfirmEmail",
    "/Identity/Account/ConfirmEmailChange",
    "/Identity/Account/RegisterConfirmation",
    "/Identity/Account/ExternalLogin"
};

var paginasParaConfiguracoes = new[]
{
    "/Identity/Account/Manage/TwoFactorAuthentication",
    "/Identity/Account/Manage/EnableAuthenticator",
    "/Identity/Account/Manage/Disable2fa",
    "/Identity/Account/Manage/ResetAuthenticator",
    "/Identity/Account/Manage/GenerateRecoveryCodes",
    "/Identity/Account/Manage/ShowRecoveryCodes",
    "/Identity/Account/Manage/SetPassword",
    "/Identity/Account/Manage/ExternalLogins"
};

app.Use(async (context, next) =>
{
    var caminho = (context.Request.Path.Value ?? "").TrimEnd('/');

    if (paginasParaLogin.Contains(caminho, StringComparer.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Identity/Account/Login");
        return;
    }

    if (paginasParaConfiguracoes.Contains(caminho, StringComparer.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Identity/Account/Manage");
        return;
    }

    await next();
});

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Garante o papel Admin e o único usuário administrador
// (e-mail e senha vêm de User Secrets / variáveis de ambiente, nunca do código).
using (var escopo = app.Services.CreateScope())
{
    await AdminSeedService.GarantirAdminAsync(escopo.ServiceProvider);
}

app.Run();