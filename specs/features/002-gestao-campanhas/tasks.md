# Tarefas — Feature 002 (Gestão de campanhas)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Domínio (TDD)

- [X] T001 [H03] `Result` e `Erro` do domínio, com testes — `Campanhas.Domain/Comum/{Result.cs, Erro.cs}`
  Commit: `✨ feat(campanhas): adiciona tipos Result e Erro do domínio`
- [X] T002 [H03] Value objects `Dinheiro` e `Periodo`, com testes — `Campanhas.Domain/Comum/*`
  Commit: `✨ feat(campanhas): adiciona value objects Dinheiro e Periodo`
- [X] T003 [H03] Agregado `Campanha` com `Criar` e eventos de domínio, com testes — `Campanhas.Domain/Campanhas/*`
  Commit: `✨ feat(campanhas): adiciona agregado Campanha com regras de criação`
- [X] T004 [H04] `Atualizar`, `Concluir` e `Cancelar` com testes das transições — `Campanhas.Domain/Campanhas/Campanha.cs`
  Commit: `✨ feat(campanhas): adiciona edição e transições de status da campanha`

## Fase 2 — Contrato do evento

- [X] T005 [P] [H03] `CampanhaAlteradaEvent` v1 e teste de snapshot JSON — `ConexaoSolidaria.Contracts/V1/CampanhaAlteradaEvent.cs`, `tests/contract/ConexaoSolidaria.ContractTests/*`
  Commit: `🏷️ feat(contracts): adiciona CampanhaAlteradaEvent v1`

## Fase 3 — Aplicação

- [X] T006 [H03] `IRelogio` (hoje em São Paulo) e `ICampanhaRepositorio` — `Campanhas.Application/Abstracoes/*`
  Commit: `🏷️ feat(campanhas): adiciona abstrações de relógio e repositório`
- [X] T007 [H03] `CriarCampanha` com validador e testes — `Campanhas.Application/Campanhas/CriarCampanha.cs`
  Commit: `✨ feat(campanhas): adiciona caso de uso de criação de campanha`
- [X] T008 [H04] `AtualizarCampanha` e `AlterarStatusCampanha` com testes — `Campanhas.Application/Campanhas/*`
  Commit: `✨ feat(campanhas): adiciona casos de uso de edição e status`
- [X] T009 [P] [H04] `ListarCampanhas` (paginação e filtro) e `ObterCampanha`, com testes — `Campanhas.Application/Campanhas/*`
  Commit: `✨ feat(campanhas): adiciona listagem e detalhe de campanhas`

## Fase 4 — Infraestrutura

- [X] T010 [H03] `CampanhasDbContext`, configuração com `xmin` e migration inicial (inclui tabelas do Outbox) — `Campanhas.Infrastructure/Persistencia/*`
  Commit: `🗃️ feat(campanhas): adiciona CampanhasDbContext e migration inicial`
- [X] T011 [H03] MassTransit com Outbox EF Core e publicação de `CampanhaAlteradaEvent`, com teste de integração — `Campanhas.Infrastructure/Mensageria/*`
  Commit: `✨ feat(campanhas): publica CampanhaAlteradaEvent via outbox`

## Fase 5 — API

- [X] T012 [H03] Endpoints `POST`, `GET` (lista e detalhe) com policy `GestorONG` — `Campanhas.Api/Endpoints/CampanhasEndpoints.cs`
  Commit: `✨ feat(campanhas): adiciona endpoints de criação e consulta de campanhas`
- [X] T013 [H04] Endpoints `PUT` com `ETag`/`If-Match` e `PATCH /status` — `Campanhas.Api/Endpoints/CampanhasEndpoints.cs`
  Commit: `✨ feat(campanhas): adiciona endpoints de edição e status com etag`
- [X] T014 [P] [H03] Métrica `campanhas_criadas_total` — `Campanhas.Application/Campanhas/CriarCampanha.cs`
  Commit: `📈 feat(campanhas): adiciona métrica de campanhas criadas`

## Fase 6 — Testes de integração e aceitação

- [X] T015 [H03] [H04] Testes de integração: 201, 400, 401, 403, 404, 409, 412, 422 e evento no Outbox — `tests/integration/Campanhas.IntegrationTests/CampanhasEndpointsTests.cs`
  Commit: `✅ test(campanhas): adiciona testes de integração da gestão de campanhas`
- [X] T016 [H03] [H04] Cenários Gherkin automatizados — `tests/e2e/ConexaoSolidaria.Bdd/Features/002-gestao-campanhas.feature`
  Commit: `✅ test(bdd): adiciona cenários de gestão de campanhas`

## Fase 7 — Interface

- [ ] T017 [H03] [H04] Páginas `/gestao/campanhas`, `/gestao/campanhas/nova` e `/gestao/campanhas/{id}` com `<AuthorizeView Roles="GestorONG">` e testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Gestao/*`
  Commit: `✨ feat(web): adiciona gestão de campanhas para gestor`

## Fase 8 — Fechamento

- [X] T018 `/code-review` e `/simplify`; cobertura ≥ 90%; `git rebase --exec` antes do PR

## Dependências

T001 → T002 → T003 → T004; T003 → T005; T006 → T007 → T008; T004 → T008; T010 → T011 → T012 → T013 → T015 → T016; T012 → T017.
