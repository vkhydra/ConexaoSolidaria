# Plano técnico — Feature 004 (Painel de transparência)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato](contracts/api.md) · [Tarefas](tasks.md)

## Resumo

Endpoint público de leitura na `Campanhas.Api`, com consulta Dapper e cache `HybridCache` + Redis. A invalidação vem do Worker ([005](../005-doacao/plan.md)) e das alterações de campanha ([002](../002-gestao-campanhas/plan.md)).

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `Dapper`, `Npgsql`, `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Caching.StackExchangeRedis` |
| Armazenamento | PostgreSQL `campanhas_db` (leitura), Redis (cache) |
| Testes | xUnit v3, Testcontainers (PostgreSQL, Redis), Reqnroll, bUnit, k6 |
| Metas | p95 < 300 ms com 50 VUs; atraso < 5 s |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de filtro, percentual, atraso e falha do cache |
| II. Domínio | ✅ | Leitura sem agregado (CQRS simples): `ICampanhaLeitura` em Application |
| III. TDD | ✅ | Caso de uso `ObterPainelTransparencia` e cálculo do percentual |
| IV. Cobertura | ✅ | Integração com PostgreSQL e Redis reais, inclusive Redis parado |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | `AllowAnonymous()` explícito; sem dados pessoais na resposta |
| VIII. Eventos | N/A | Só lê; a invalidação é da feature 005 |
| IX. Observabilidade | ✅ | Painel de latência; métrica de acerto de cache |
| X. Infra | ✅ | Redis no compose e no Kustomize |
| XI. Simplicidade | ✅ | Uma consulta, um cache |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/Services/Campanhas/
├── Campanhas.Application/
│   ├── Abstracoes/{ICampanhaLeitura.cs, ICachePainel.cs}
│   └── Transparencia/{ObterPainelTransparencia.cs, PainelItemResponse.cs}
├── Campanhas.Infrastructure/
│   ├── Leitura/CampanhaLeituraDapper.cs
│   └── Cache/CachePainelHybrid.cs
└── Campanhas.Api/Endpoints/TransparenciaEndpoints.cs
tests/unit/Campanhas.UnitTests/Transparencia/*
tests/integration/Campanhas.IntegrationTests/TransparenciaTests.cs
tests/e2e/ConexaoSolidaria.Bdd/Features/004-painel-transparencia.feature
tests/e2e/load/painel.js
```

## Design

1. `GET` → `ObterPainelTransparencia.ExecutarAsync`.
2. `HybridCache.GetOrCreateAsync(chave, fábrica)`; a fábrica chama `ICampanhaLeitura.ListarAtivasAsync(hoje)`.
3. Exceção do Redis → captura, `Warning`, chama a fábrica direto.
4. Endpoint aplica `Cache-Control: public, max-age=10`.

## Observabilidade

| Sinal | Definição |
|---|---|
| Métrica | `painel_cache_total{resultado="acerto|falta|erro"}`; latência pelo `http_request_duration_seconds` |
| Alerta | `LatenciaPainelAlta` ([runbook](../../../docs/runbooks/latencia-painel-alta.md)) |
| Trace | Span `ObterPainelTransparencia` com evento de cache |

## Riscos

| Risco | Mitigação |
|---|---|
| L1 desatualizado em outra réplica | L1 de só 5 s |
| Redis lento sem cair | Timeout de 200 ms na conexão do Redis |
