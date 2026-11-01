# Tarefas — Feature 006 (Busca de campanhas)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Aplicação (TDD)

- [X] T001 [H08] `IBuscaCampanhas`, `IIndiceCampanhas`, `BuscaResponse` e caso de uso `BuscarCampanhas` com testes (validação de `q`, paginação, indisponível) — `Campanhas.Application/Busca/*`
  Commit: `✨ feat(campanhas): adiciona caso de uso de busca de campanhas`
- [X] T002 [P] [H08] Mapeamento `CampanhaAlteradaEvent` → documento, com testes — `Campanhas.Application/Busca/*`
  Commit: `✨ feat(campanhas): adiciona mapeamento de campanha para o índice`

## Fase 2 — Infraestrutura

- [X] T003 [H08] Criação do índice `campanhas-v1` com analyzer `pt_br` e alias `campanhas` — `Campanhas.Infrastructure/Busca/MapeamentoIndice.cs`
  Commit: `🗃️ feat(campanhas): adiciona índice de campanhas no elasticsearch`
- [X] T004 [H08] `IndiceCampanhasElasticsearch` (upsert com versão externa) — `Campanhas.Infrastructure/Busca/IndiceCampanhasElasticsearch.cs`
  Commit: `✨ feat(campanhas): adiciona indexação de campanhas no elasticsearch`
- [X] T005 [H08] `BuscaCampanhasElasticsearch` com fuzzy, pesos, filtro e destaque — `Campanhas.Infrastructure/Busca/BuscaCampanhasElasticsearch.cs`
  Commit: `✨ feat(campanhas): adiciona busca fuzzy de campanhas`

## Fase 3 — API e consumidor

- [X] T006 [H08] `IndexarCampanhaConsumer` na fila `indexar-campanha` com retentativas — `Campanhas.Api/Consumidores/IndexarCampanhaConsumer.cs`
  Commit: `✨ feat(campanhas): adiciona consumidor de indexação de campanhas`
- [X] T007 [H08] Endpoint `GET /campanhas/busca` (`AllowAnonymous`) com 503 em falha — `Campanhas.Api/Endpoints/BuscaEndpoints.cs`
  Commit: `✨ feat(campanhas): adiciona endpoint de busca de campanhas`
- [ ] T008 [H08] Endpoint `POST /admin/reindexar` (`GestorONG`) com troca de alias — `Campanhas.Api/Endpoints/AdminEndpoints.cs`, `Campanhas.Application/Busca/Reindexar.cs`
  Commit: `✨ feat(campanhas): adiciona reindexação de campanhas`
- [ ] T009 [P] [H08] Health check `Degraded` e métricas de busca — `Campanhas.Api/Program.cs`, `Campanhas.Application/Busca/*`
  Commit: `🩺 feat(campanhas): adiciona health check e métricas da busca`

## Fase 4 — Testes de integração, aceitação e carga

- [ ] T010 [H08] Testes de integração: indexa ao criar, erro de digitação, pesos, ignora cancelada, evento fora de ordem, 503 com Elasticsearch parado — `tests/integration/Campanhas.IntegrationTests/BuscaTests.cs`
  Commit: `✅ test(campanhas): adiciona testes de integração da busca`
- [ ] T011 [H08] Cenários Gherkin automatizados — `tests/e2e/ConexaoSolidaria.Bdd/Features/006-busca-campanhas.feature`
  Commit: `✅ test(bdd): adiciona cenários de busca de campanhas`
- [ ] T012 [P] [H08] Script k6 com 30 VUs, termos com erro de digitação e threshold p95 < 400 ms — `tests/e2e/load/busca.js`
  Commit: `✅ test(load): adiciona teste de carga da busca com k6`

## Fase 5 — Interface

- [ ] T013 [H08] Página `/busca` com trecho destacado sanitizado e mensagem de indisponibilidade, com testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Busca.razor(.cs)`
  Commit: `✨ feat(web): adiciona busca de campanhas`

## Fase 6 — Fechamento

- [ ] T014 `/code-review`; cobertura ≥ 90%; `git rebase --exec` antes do PR

## Dependências

T001, T002 → T004, T005; T003 → T004 → T006; T005 → T007 → T010 → T011; T004 → T008; T007 → T012, T013.
