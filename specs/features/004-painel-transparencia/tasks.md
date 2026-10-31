# Tarefas — Feature 004 (Painel de transparência)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Aplicação (TDD)

- [X] T001 [H05] `PainelItemResponse`, `ICampanhaLeitura`, `ICachePainel` e caso de uso `ObterPainelTransparencia` com testes (percentual, lista vazia, falha do cache) — `Campanhas.Application/Transparencia/*`
  Commit: `✨ feat(campanhas): adiciona caso de uso do painel de transparência`

## Fase 2 — Infraestrutura

- [X] T002 [H05] `CampanhaLeituraDapper` com a consulta do painel e teste de integração (filtros de status e período) — `Campanhas.Infrastructure/Leitura/CampanhaLeituraDapper.cs`
  Commit: `✨ feat(campanhas): adiciona consulta do painel com dapper`
- [X] T003 [H05] `CachePainelHybrid` com `HybridCache` + Redis, chave versionada e remoção — `Campanhas.Infrastructure/Cache/CachePainelHybrid.cs`
  Commit: `⚡️ feat(campanhas): adiciona cache do painel com hybridcache e redis`
- [X] T004 [H05] Invalidação do cache ao criar ou alterar campanha — `Campanhas.Application/Campanhas/*`
  Commit: `⚡️ feat(campanhas): invalida cache do painel ao alterar campanha`

## Fase 3 — API

- [X] T005 [H05] Endpoint `GET /transparencia/campanhas` com `AllowAnonymous` e `Cache-Control` — `Campanhas.Api/Endpoints/TransparenciaEndpoints.cs`
  Commit: `✨ feat(campanhas): adiciona endpoint do painel de transparência`
- [X] T006 [P] [H05] Métrica `painel_cache_total{resultado}` — `Campanhas.Infrastructure/Cache/CachePainelHybrid.cs`
  Commit: `📈 feat(campanhas): adiciona métrica de cache do painel`

## Fase 4 — Testes de integração, aceitação e carga

- [X] T007 [H05] Testes de integração: só ativas no período, ordenação, percentual, Redis parado, cabeçalho de cache — `tests/integration/Campanhas.IntegrationTests/TransparenciaTests.cs`
  Commit: `✅ test(campanhas): adiciona testes de integração do painel`
- [ ] T008 [H05] Cenários Gherkin (exceto "após doação", entregue com a feature 005) — `tests/e2e/ConexaoSolidaria.Bdd/Features/004-painel-transparencia.feature`
  Commit: `✅ test(bdd): adiciona cenários do painel de transparência`
- [ ] T009 [P] [H05] Script k6 com threshold p95 < 300 ms e 50 VUs — `tests/e2e/load/painel.js`
  Commit: `✅ test(load): adiciona teste de carga do painel com k6`

## Fase 5 — Interface

- [ ] T010 [H05] Página `/transparencia` (SSR estático) com barra de progresso e aviso de atualização, com testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Transparencia.razor(.cs)`
  Commit: `✨ feat(web): adiciona página de transparência`

## Fase 6 — Fechamento

- [ ] T011 `/code-review`; cobertura ≥ 90%; `git rebase --exec` antes do PR

## Dependências

T001 → T002, T003 → T005 → T007 → T008; T003 → T004; T005 → T009, T010.
