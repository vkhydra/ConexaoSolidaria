using Campanhas.Api.Endpoints;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Doacoes;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using ConexaoSolidaria.ServiceDefaults.Erros;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.Telemetria;
using FluentValidation;

const string Servico = "campanhas-api";

var builder = WebApplication.CreateBuilder(args);

builder.AddLogsEstruturados(Servico);
builder.AddTelemetria(Servico);
builder.AddSaude();
builder.AddTratamentoErros();
builder.AddAutenticacaoJwt();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCampanhasInfraestrutura(builder.Configuration);
builder.Services.AddSingleton<IValidator<CampanhaRequest>, CampanhaRequestValidator>();
builder.Services.AddSingleton<IValidator<AlterarStatusRequest>, AlterarStatusRequestValidator>();
builder.Services.AddSingleton<IValidator<ListarCampanhasRequest>, ListarCampanhasRequestValidator>();
builder.Services.AddSingleton<IValidator<DoacaoRequest>, DoacaoRequestValidator>();
builder.Services.AddSingleton<InvalidadorDoPainel>();
builder.Services.AddScoped<CriarCampanha>();
builder.Services.AddScoped<AtualizarCampanha>();
builder.Services.AddScoped<AlterarStatusCampanha>();
builder.Services.AddScoped<ListarCampanhas>();
builder.Services.AddScoped<ObterCampanha>();
builder.Services.AddScoped<ObterPainelTransparencia>();
builder.Services.AddScoped<ReceberDoacao>();

var app = builder.Build();

app.UsarTratamentoErros();
app.UsarLogsDeRequisicoes();
app.UseAuthentication();
app.UseAuthorization();
app.MapearSaude();
app.MapearMetricas();
app.MapearCampanhas();
app.MapearTransparencia();
app.MapearDoacoes();

app.Run();

/// <summary>
/// Ponto de entrada da Campanhas.Api, exposto para os testes de integração.
/// </summary>
public partial class Program
{
}
