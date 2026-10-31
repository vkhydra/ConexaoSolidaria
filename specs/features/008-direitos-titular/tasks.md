# Tarefas — Feature 008 (Direitos do titular)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Domínio (TDD)

- [X] T001 [H10] `Usuario.Anonimizar` idempotente, com testes (anonimizar, repetir, senha após anonimizar) — `Identity.Domain/Usuarios/Usuario.cs`
  Commit: `✨ feat(identity): adiciona anonimização do usuário`

## Fase 2 — Aplicação

- [X] T002 [H10] Casos de uso `ObterMeusDados` e `AnonimizarMeusDados` com testes — `Identity.Application/Titular/*`
  Commit: `✨ feat(identity): adiciona casos de uso de acesso e anonimização do titular`

## Fase 3 — API

- [X] T003 [H10] Endpoints `GET` e `DELETE /doadores/me` com policy `Doador` e `Cache-Control: no-store` — `Identity.Api/Endpoints/TitularEndpoints.cs`
  Commit: `✨ feat(identity): adiciona endpoints de acesso e anonimização do titular`
- [ ] T004 [P] [H10] Log de auditoria `TitularAnonimizado` e métrica `titulares_anonimizados_total` — `Identity.Application/Titular/AnonimizarMeusDados.cs`
  Commit: `🔊 feat(identity): adiciona auditoria da anonimização do titular`

## Fase 4 — Testes de integração e aceitação

- [ ] T005 [H10] Testes de integração: 200 com CPF mascarado, 204, colunas anonimizadas no banco, login seguinte 401, `/me` com token antigo 401, e-mail reutilizável — `tests/integration/Identity.IntegrationTests/TitularTests.cs`
  Commit: `✅ test(identity): adiciona testes de integração dos direitos do titular`
- [ ] T006 [H10] Cenários Gherkin (inclui total da campanha mantido) — `tests/e2e/ConexaoSolidaria.Bdd/Features/008-direitos-titular.feature`
  Commit: `✅ test(bdd): adiciona cenários de direitos do titular`

## Fase 5 — Interface

- [ ] T007 [H10] Página `/meus-dados` com confirmação antes de excluir e saída da sessão após 204, com testes bUnit — `ConexaoSolidaria.Web/Components/Pages/MeusDados.razor(.cs)`
  Commit: `✨ feat(web): adiciona página de dados do titular`

## Fase 6 — Fechamento

- [ ] T008 `/security-review` (foco LGPD) e `/code-review`; cobertura ≥ 90%; `git rebase --exec`

## Dependências

T001 → T002 → T003 → T005 → T006; T003 → T004, T007.
