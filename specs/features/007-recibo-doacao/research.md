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
- **Resultado do spike (2026-10-31, parcial):** o envelope real foi capturado com o MassTransit 8.5.12 (`ReceiveContext.GetBody()` no Test Harness, código descartado). Achados:
  - O corpo é JSON com `messageType` (lista, com `urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent`), `message` (campos em camelCase) e `headers` (vazio).
  - **`decimal` vai como texto** (`"valor": "150"`), então o leitor precisa aceitar números escritos como string (`JsonNumberHandling.AllowReadingFromString`).
  - **`traceparent` não vem em `headers` do envelope**: o MassTransit o grava só no cabeçalho AMQP. Com o parâmetro `string`, a função não o enxerga; a continuidade do trace fica a cargo da instrumentação do gatilho, e o recibo leva o `traceId` do próprio span da função. O contrato abaixo foi corrigido.
  - **Não validado:** a execução do host da função (gatilho RabbitMQ entregando o `string`), porque o Azure Functions Core Tools (`func`) não está instalado e o Docker estava indisponível. A hipótese segue a documentação do gatilho (parâmetros `string`, `byte[]` ou POCO). Validar ao empacotar a função (T007) e na Fase 9.

## R3 — Deduplicação sem banco próprio

- **Decisão:** `SET conexao:recibos:v1:<doacaoId> 1 NX EX 604800` no Redis antes de registrar o recibo.
- **Justificativa:** operação atômica; a função continua sem estado próprio; 7 dias cobrem qualquer reentrega.
- **Alternativas:** tabela no PostgreSQL (acoplaria a função ao banco de Campanhas); sem deduplicação (recibos repetidos).

## R4 — Versão do .NET na função

- **Decisão:** .NET 10 isolated; se o runtime do Functions ainda não suportar, .NET 8 isolated só neste projeto.
- **Verificação:** MCP Microsoft Learn, "Supported languages in Azure Functions", no início do spike.
- **Resultado do spike:** o projeto segue em .NET 10 isolated (`TargetFramework` herdado da solução); a disponibilidade da imagem base `mcr.microsoft.com/azure-functions/dotnet-isolated` para o .NET 10 será confirmada ao escrever o Dockerfile (T007). Se faltar, usar .NET 8 só neste projeto.

## R5 — Declaração da fila

- **Decisão:** fila `notificacoes-recibo` e binding com a exchange `ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent` declarados em `deploy/rabbitmq/definitions.json`.
- **Justificativa:** a função não usa MassTransit e não cria a topologia sozinha.
