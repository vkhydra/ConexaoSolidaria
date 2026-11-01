using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notificacoes.Function;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddNotificacoes(builder.Configuration);

// Traces e logs seguem o padrão OpenTelemetry; o coletor vem de OTEL_EXPORTER_OTLP_ENDPOINT.
builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults().UseOtlpExporter();

builder.Build().Run();
