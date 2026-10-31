using Campanhas.Application.Arrecadacao;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Mensageria;
using ConexaoSolidaria.ServiceDefaults.Logs;
using ConexaoSolidaria.ServiceDefaults.Saude;
using ConexaoSolidaria.ServiceDefaults.Telemetria;

const string Servico = "arrecadacao-worker";

var builder = WebApplication.CreateBuilder(args);

// O Worker só serve as sondas de saúde e as métricas; a porta é a 9090, salvo quando a plataforma define as URLs.
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://+:9090");
}

builder.AddLogsEstruturados(Servico);
builder.AddTelemetria(Servico, "MassTransit");
builder.AddSaude();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCampanhasPersistencia(builder.Configuration);
builder.Services.AddCampanhasArrecadacao(builder.Configuration);
builder.Services.AddScoped<ProcessarDoacao>();
builder.Services.AddCampanhasMensageria(builder.Configuration);

var app = builder.Build();

app.UsarLogsDeRequisicoes();
app.MapearSaude();
app.MapearMetricas();

app.Run();

/// <summary>
/// Ponto de entrada do Arrecadacao.Worker, exposto para os testes.
/// </summary>
public partial class Program
{
}
