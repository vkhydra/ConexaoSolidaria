# Plano técnico — Feature 007 (Recibo de doação)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Eventos](contracts/eventos.md) · [Tarefas](tasks.md)

## Resumo

Função `EmitirRecibo` (Azure Functions isolated worker) com gatilho RabbitMQ na fila `notificacoes-recibo`. Lê o envelope do MassTransit, deduplica no Redis e registra `ReciboEmitido` em log estruturado. Roda em container e escala de 0 a 3 réplicas com KEDA.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `Microsoft.Azure.Functions.Worker`, `Microsoft.Azure.Functions.Worker.Extensions.RabbitMQ`, `StackExchange.Redis`, `OpenTelemetry` |
| Armazenamento | Redis (só deduplicação) |
| Testes | xUnit v3, NSubstitute, Testcontainers (Redis), captura de logs |
| Plataforma | Container `azure-functions/dotnet-isolated` + KEDA `ScaledObject` (0–3, fila > 10) |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de recibo, duplicidade e escala |
| II. Domínio | ✅ | Contexto Notificações sem estado; só consome contrato publicado (Conformist) |
| III. TDD | ✅ | Leitor de envelope e emissor de recibo com testes primeiro |
| IV. Cobertura | ✅ | Unidade (envelope, deduplicação, log) + integração com Redis |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | Sem dados pessoais; credenciais do RabbitMQ em Secret |
| VIII. Eventos | ✅ | Consumidor idempotente |
| IX. Observabilidade | ✅ | Log de negócio, trace continuado, réplicas no dashboard |
| X. Infra | ✅ | Dockerfile, Deployment com 0 réplicas, `ScaledObject`, `definitions.json` |
| XI. Simplicidade | ✅ | Sem banco próprio |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/Functions/Notificacoes.Function/
├── Program.cs
├── EmitirRecibo.cs
├── Recibos/{LeitorEnvelopeMassTransit.cs, EmissorRecibo.cs, IDeduplicadorRecibos.cs, DeduplicadorRecibosRedis.cs}
├── host.json
└── Dockerfile
deploy/rabbitmq/definitions.json
deploy/k8s/base/apps/notificacoes-function/{deployment.yaml, scaledobject.yaml, triggerauthentication.yaml}
tests/unit/Notificacoes.Function.UnitTests/*
```

## Design

1. KEDA observa `notificacoes-recibo`; com mensagens, sobe a réplica.
2. Gatilho entrega o envelope; `LeitorEnvelopeMassTransit` valida `messageType` e desserializa `message`.
3. `DeduplicadorRecibosRedis.TentarRegistrarAsync(doacaoId)` (`SET NX EX`); falso → termina sem log de recibo.
4. `EmissorRecibo` registra `ReciboEmitido` com os campos permitidos.
5. Sem mensagens por 60 s (`cooldownPeriod`) → 0 réplicas.

## Observabilidade

| Sinal | Definição |
|---|---|
| Log | `ReciboEmitido` (`Information`), `ReciboDuplicadoIgnorado` (`Debug`) |
| Métrica | Réplicas da função no dashboard (`kube_deployment_status_replicas_available`) |
| Trace | Span `EmitirRecibo` filho do trace da doação |

## Riscos

| Risco | Mitigação |
|---|---|
| Runtime sem .NET 10 | .NET 8 isolated só nesta função ([pesquisa R4](research.md)) |
| Envelope incompatível | Spike R2; plano B com JSON cru |
