# Pesquisa — Feature 007 (Recibo de doação)

[← Spec](spec.md)

## R1 — Modelo de execução serverless local

- **Decisão:** Azure Functions isolated worker em container (`mcr.microsoft.com/azure-functions/dotnet-isolated`), escalado pelo KEDA no Kubernetes.
- **Justificativa:** modelo de programação serverless da Fase 3, escala a zero e roda sem custo no Minikube.
- **Alternativas:** AWS Lambda com LocalStack (exigiria ponte RabbitMQ → SQS); plano Consumption da Azure (não suporta gatilho RabbitMQ).
- **Registro:** [ADR 0010](../../../docs/adr/0010-notificacoes-serverless-com-keda.md).

## R2 — Leitura do envelope do MassTransit (spike, time-box 1 dia)

- **Pergunta:** o gatilho RabbitMQ do isolated worker entrega o corpo bruto do envelope?
- **Hipótese:** sim; a função recebe `string`/`byte[]` e desserializa só o campo `message` com `System.Text.Json`.
- **Plano B:** configurar o MassTransit para publicar este evento em JSON "cru" (`UseRawJsonSerializer` no endpoint) ([riscos](../../../docs/entrega-e-riscos.md)).

## R3 — Deduplicação sem banco próprio

- **Decisão:** `SET conexao:recibos:v1:<doacaoId> 1 NX EX 604800` no Redis antes de registrar o recibo.
- **Justificativa:** operação atômica; a função continua sem estado próprio; 7 dias cobrem qualquer reentrega.
- **Alternativas:** tabela no PostgreSQL (acoplaria a função ao banco de Campanhas); sem deduplicação (recibos repetidos).

## R4 — Versão do .NET na função

- **Decisão:** .NET 10 isolated; se o runtime do Functions ainda não suportar, .NET 8 isolated só neste projeto.
- **Verificação:** MCP Microsoft Learn, "Supported languages in Azure Functions", no início do spike.

## R5 — Declaração da fila

- **Decisão:** fila `notificacoes-recibo` e binding com a exchange `ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent` declarados em `deploy/rabbitmq/definitions.json`.
- **Justificativa:** a função não usa MassTransit e não cria a topologia sozinha.
