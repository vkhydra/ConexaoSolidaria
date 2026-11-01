using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.Web.Components;
using ConexaoSolidaria.Web.Servicos;
using Microsoft.AspNetCore.Authentication.Cookies;

if (SondaDeContainer.Solicitada(args))
{
    return await SondaDeContainer.ExecutarAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddServicosDaApi(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/entrar";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Lax;
        opcoes.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        opcoes.SlidingExpiration = false;
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapearSaude();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapearSessao();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
return 0;

/// <summary>
/// Ponto de entrada da interface web, exposto para os testes.
/// </summary>
public partial class Program
{
}
