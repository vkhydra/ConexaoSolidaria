using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using ConexaoSolidaria.ServiceDefaults.Erros;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.Telemetria;
using FluentValidation;
using Identity.Api.Endpoints;
using Identity.Application.Autenticacao;
using Identity.Application.Doadores;
using Identity.Application.Titular;
using Identity.Infrastructure;

if (SondaDeContainer.Solicitada(args))
{
    return await SondaDeContainer.ExecutarAsync();
}

const string Servico = "identity-api";

var builder = WebApplication.CreateBuilder(args);

builder.AddLogsEstruturados(Servico);
builder.AddTelemetria(Servico);
builder.AddSaude();
builder.AddTratamentoErros();
builder.AddAutenticacaoJwt();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIdentityInfraestrutura(builder.Configuration);
builder.Services.AddSingleton<IValidator<AutenticarRequest>, AutenticarRequestValidator>();
builder.Services.AddScoped<Autenticar>();
builder.Services.AddSingleton<IValidator<CadastrarDoadorRequest>, CadastrarDoadorRequestValidator>();
builder.Services.AddSingleton(new CadastrarDoadorOpcoes(builder.Configuration["Lgpd:VersaoPolitica"] ?? "2026-10"));
builder.Services.AddScoped<CadastrarDoador>();
builder.Services.AddScoped<ObterMeusDados>();
builder.Services.AddScoped<AnonimizarMeusDados>();

var app = builder.Build();

app.UsarTratamentoErros();
app.UsarLogsDeRequisicoes();
app.UseAuthentication();
app.UseAuthorization();
app.MapearSaude();
app.MapearMetricas();
app.MapearAutenticacao();
app.MapearDoadores();
app.MapearTitular();

app.Run();
return 0;

/// <summary>
/// Ponto de entrada da Identity.Api, exposto para os testes de integração.
/// </summary>
public partial class Program
{
}
