# Plano técnico — Feature 006 (Busca de campanhas)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato](contracts/api.md) · [Tarefas](tasks.md)

## Resumo

Índice `campanhas` no Elasticsearch alimentado pelo `IndexarCampanhaConsumer` a partir de `CampanhaAlteradaEvent`. Endpoint público de busca fuzzy e endpoint de reindexação para o gestor.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `Elastic.Clients.Elasticsearch`, `MassTransit.RabbitMQ`, `AspNetCore.HealthChecks.Elasticsearch` |
| Armazenamento | Elasticsearch 9 (índice derivado; fonte da verdade é o PostgreSQL) |
| Testes | xUnit v3, Testcontainers (Elasticsearch, RabbitMQ, PostgreSQL), MassTransit Test Harness, Reqnroll, bUnit, k6 |
| Metas | p95 < 400 ms; indexação < 5 s |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de fuzzy, relevância, filtro e falha |
| II. Domínio | ✅ | `IBuscaCampanhas` em Application; Elasticsearch só na infraestrutura |
| III. TDD | ✅ | Caso de uso `BuscarCampanhas` e mapeamento do evento para o documento |
| IV. Cobertura | ✅ | Integração com Elasticsearch real (inclusive parado) |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | Busca pública só de campanhas ativas; Elasticsearch com API key e sem exposição externa |
| VIII. Eventos | ✅ | Consumidor idempotente por versão externa |
| IX. Observabilidade | ✅ | Health `Degraded`; métrica de buscas e falhas |
| X. Infra | ✅ | StatefulSet do Elasticsearch com heap de 512 MB |
| XI. Simplicidade | ✅ | Consumidor na própria API (sem serviço novo) |
| XII. Português | ✅ | Analyzer em português |

## Estrutura de código

```text
src/Services/Campanhas/
├── Campanhas.Application/Busca/{BuscarCampanhas.cs, IBuscaCampanhas.cs, IIndiceCampanhas.cs, BuscaResponse.cs, Reindexar.cs}
├── Campanhas.Infrastructure/Busca/{IndiceCampanhasElasticsearch.cs, BuscaCampanhasElasticsearch.cs, MapeamentoIndice.cs}
└── Campanhas.Api/
    ├── Consumidores/IndexarCampanhaConsumer.cs
    └── Endpoints/{BuscaEndpoints.cs, AdminEndpoints.cs}
tests/unit/Campanhas.UnitTests/Busca/*
tests/integration/Campanhas.IntegrationTests/BuscaTests.cs
tests/e2e/ConexaoSolidaria.Bdd/Features/006-busca-campanhas.feature
tests/e2e/load/busca.js
```

## Design

1. `CampanhaAlteradaEvent` → `IndexarCampanhaConsumer` → `IIndiceCampanhas.UpsertAsync(documento, versao)`.
2. Conflito de versão (evento antigo) → ignorado e confirmado.
3. Falha do Elasticsearch → retentativas do MassTransit → fila `indexar-campanha_error`.
4. `GET /campanhas/busca` → `BuscarCampanhas` → `IBuscaCampanhas.BuscarAsync(q, page, pageSize)`; exceção de transporte → 503.
5. `POST /admin/reindexar` → cria índice novo, carrega campanhas do PostgreSQL em lotes de 500, troca o alias.

## Observabilidade

| Sinal | Definição |
|---|---|
| Métricas | `buscas_total{resultado="ok|vazio|indisponivel"}`, `campanhas_indexadas_total` |
| Health | Elasticsearch como `Degraded` no `/health/ready` |
| Trace | Spans `BuscarCampanhas` e `IndexarCampanha` (instrumentação do cliente Elastic) |

## Riscos

| Risco | Mitigação |
|---|---|
| Memória do Elasticsearch no Minikube | Heap de 512 MB; corte previsto na [ordem de corte](../../../docs/entrega-e-riscos.md) |
| `vm.max_map_count` no Linux | Passo descrito nos [pré-requisitos](../../../docs/pre-requisitos.md) |
