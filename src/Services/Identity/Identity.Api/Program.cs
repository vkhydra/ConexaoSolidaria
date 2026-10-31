using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using ConexaoSolidaria.ServiceDefaults.Erros;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.Telemetria;
using FluentValidation;
using Identity.Api.Endpoints;
using Identity.Application.Autenticacao;
using Identity.Infrastructure;

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

var app = builder.Build();

app.UsarTratamentoErros();
app.UsarLogsDeRequisicoes();
app.UseAuthentication();
app.UseAuthorization();
app.MapearSaude();
app.MapearMetricas();
app.MapearAutenticacao();

app.Run();

/// <summary>
/// Ponto de entrada da Identity.Api, exposto para os testes de integração.
/// </summary>
public partial class Program
{
}
