# Plano técnico — Feature 005 (Doação)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato HTTP](contracts/api.md) · [Eventos](contracts/eventos.md) · [Tarefas](tasks.md)

## Resumo

A `Campanhas.Api` valida a doação, grava `DoacaoRecebidaEvent` no Outbox e responde 202. O `Arrecadacao.Worker` consome o evento e, numa transação, registra a idempotência, soma o total e grava `DoacaoProcessadaEvent` no Outbox; depois grava o livro de doações no MongoDB e invalida o cache do painel.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `MassTransit.RabbitMQ`, `MassTransit.EntityFrameworkCore` (8.x), `MongoDB.Driver`, `Dapper`, `Microsoft.Extensions.Caching.StackExchangeRedis` |
| Armazenamento | PostgreSQL `campanhas_db`, MongoDB `arrecadacao`, Redis |
| Testes | xUnit v3, MassTransit Test Harness, Testcontainers (PostgreSQL, RabbitMQ, MongoDB, Redis), Reqnroll, bUnit, k6 |
| Metas | API p95 < 500 ms; atraso p95 < 5 s; zero perda e zero duplicidade |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de aceite, recusa, idempotência, reentrega |
| II. Domínio | ✅ | `PodeReceberDoacao` e `RegistrarDoacaoProcessada` no agregado |
| III. TDD | ✅ | Regras do agregado e casos de uso `ReceberDoacao` e `ProcessarDoacao` |
| IV. Cobertura | ✅ | Test Harness + Testcontainers cobrem sucesso, duplicata, falha transitória e permanente |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | Policy `Doador`; eventos só com IDs |
| VIII. Eventos | ✅ | Outbox nos dois lados; consumidor idempotente; contratos com snapshot |
| IX. Observabilidade | ✅ | Métricas de negócio, trace API → Worker, alertas de fila e DLQ |
| X. Infra | ✅ | Worker com Deployment e `ScaledObject` (KEDA 1–4) |
| XI. Simplicidade | ✅ | Sem saga (um passo de negócio) |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/BuildingBlocks/ConexaoSolidaria.Contracts/V1/{DoacaoRecebidaEvent.cs, DoacaoProcessadaEvent.cs}
src/Services/Campanhas/
├── Campanhas.Domain/Campanhas/Campanha.cs                 # PodeReceberDoacao, RegistrarDoacaoProcessada
├── Campanhas.Application/
│   ├── Doacoes/{ReceberDoacao.cs, DoacaoRequest.cs, DoacaoAceitaResponse.cs, DoacaoRequestValidator.cs}
│   └── Arrecadacao/{ProcessarDoacao.cs, ILivroDoacoes.cs, IRegistroArrecadacao.cs}
├── Campanhas.Infrastructure/
│   ├── Persistencia/{Configuracoes/DoacaoProcessadaConfiguration.cs, ChaveIdempotenciaConfiguration.cs, Migrations/*}
│   ├── Arrecadacao/{RegistroArrecadacaoSql.cs, LivroDoacoesMongo.cs}
│   └── Idempotencia/LimpezaChavesIdempotenciaHostedService.cs
├── Campanhas.Api/Endpoints/DoacoesEndpoints.cs
└── Arrecadacao.Worker/{Program.cs, Consumidores/DoacaoRecebidaConsumer.cs, Dockerfile}
tests/unit/{Campanhas.UnitTests, Arrecadacao.Worker.UnitTests}/…
tests/integration/{Campanhas.IntegrationTests/DoacoesTests.cs, Arrecadacao.Worker.IntegrationTests/*}
tests/contract/ConexaoSolidaria.ContractTests/{DoacaoRecebidaEventTests.cs, DoacaoProcessadaEventTests.cs}
tests/e2e/ConexaoSolidaria.Bdd/Features/005-doacao.feature
tests/e2e/load/doacao.js
```

## Design

```mermaid
sequenceDiagram
  autonumber
  actor D as Doador
  participant C as Campanhas.Api
  participant PG as campanhas_db
  participant MQ as RabbitMQ
  participant W as Arrecadacao.Worker
  participant MG as MongoDB
  participant R as Redis
  D->>C: POST /doacoes (JWT Doador)
  C->>PG: lê campanha (apta?)
  C->>PG: Outbox(DoacaoRecebidaEvent) [+ chave de idempotência]
  C-->>D: 202 {doacaoId}
  PG-->>MQ: entrega do Outbox
  MQ->>W: DoacaoRecebidaEvent
  W->>PG: BEGIN; INSERT doacoes_processadas; UPDATE total; Outbox(DoacaoProcessadaEvent); COMMIT
  W->>MG: upsert lançamento (sempre, também na reentrega)
  W->>R: remove chave do painel
  PG-->>MQ: entrega de DoacaoProcessadaEvent
```

Pontos de consistência: o passo 3 e o passo 7 são atômicos; os passos 8 e 9 são idempotentes e sempre executados ([arquitetura](../../product/arquitetura.md)).

## Observabilidade

| Sinal | Definição |
|---|---|
| Métricas | `doacoes_recebidas_total`, `doacoes_recusadas_total{motivo}`, `doacoes_processadas_total{resultado}`, `doacoes_valor_reais_total`, `doacao_atraso_processamento_segundos` |
| Alertas | `MensagensNaDLQ`, `FilaDoacoesAcumulando` ([runbooks](../../../docs/runbooks/)) |
| Trace | `ReceberDoacao` → publicação → `ProcessarDoacao` no mesmo trace (`traceparent` no header) |
| Escala | KEDA no Worker pela fila `doacao-recebida` (> 20 mensagens por réplica) |

## Riscos

| Risco | Mitigação |
|---|---|
| Atraso do Outbox (polling) | Intervalo de 1 s; métrica de atraso |
| Mensagem envenenada | Fila `_error` + alerta + runbook |
| MongoDB divergente do total | Conferência por soma (SC-005) no teste de integração |
