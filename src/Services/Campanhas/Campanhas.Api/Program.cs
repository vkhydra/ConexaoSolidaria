using Campanhas.Api.Consumidores;
using Campanhas.Api.Endpoints;
using Campanhas.Api.Saude;
using Campanhas.Application.Busca;
using Campanhas.Application.Campanhas;
using Campanhas.Application.Doacoes;
using Campanhas.Application.Transparencia;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Mensageria;
using ConexaoSolidaria.ServiceDefaults.Autenticacao;
using ConexaoSolidaria.ServiceDefaults.Erros;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.Telemetria;
using FluentValidation;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string Servico = "campanhas-api";

var builder = WebApplication.CreateBuilder(args);

builder.AddLogsEstruturados(Servico);
builder.AddTelemetria(Servico);
builder.AddSaude().AddCheck<ElasticsearchHealthCheck>(ElasticsearchHealthCheck.Nome, HealthStatus.Degraded, [ExtensoesSaude.EtiquetaProntidao]);
builder.AddTratamentoErros();
builder.AddAutenticacaoJwt();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(PoliticaDeRetentativa.Padrao);
builder.Services.AddCampanhasInfraestrutura(builder.Configuration, registro => registro.AddConsumer<IndexarCampanhaConsumer, IndexarCampanhaConsumerDefinition>());
builder.Services.AddSingleton<IValidator<CampanhaRequest>, CampanhaRequestValidator>();
builder.Services.AddSingleton<IValidator<AlterarStatusRequest>, AlterarStatusRequestValidator>();
builder.Services.AddSingleton<IValidator<ListarCampanhasRequest>, ListarCampanhasRequestValidator>();
builder.Services.AddSingleton<IValidator<DoacaoRequest>, DoacaoRequestValidator>();
builder.Services.AddSingleton<IValidator<BuscarCampanhasRequest>, BuscarCampanhasRequestValidator>();
builder.Services.AddSingleton<InvalidadorDoPainel>();
builder.Services.AddScoped<CriarCampanha>();
builder.Services.AddScoped<AtualizarCampanha>();
builder.Services.AddScoped<AlterarStatusCampanha>();
builder.Services.AddScoped<ListarCampanhas>();
builder.Services.AddScoped<ObterCampanha>();
builder.Services.AddScoped<ObterPainelTransparencia>();
builder.Services.AddScoped<ReceberDoacao>();
builder.Services.AddScoped<BuscarCampanhas>();
builder.Services.AddSingleton<Reindexar>();

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
app.MapearBusca();
app.MapearAdmin();

app.Run();

/// <summary>
/// Ponto de entrada da Campanhas.Api, exposto para os testes de integração.
/// </summary>
public partial class Program
{
}
