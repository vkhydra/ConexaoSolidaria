# Tarefas — Feature 009 (Testes unitários e cobertura no CI)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md). Estas tarefas fazem parte da Etapa 3 do [plano de execução](../../../docs/plano-de-execucao.md) e vêm **antes** das features 001–008.

## Fase 1 — Base de testes

- [X] T001 [H11] Projetos `tests/unit/*.UnitTests` com xUnit v3, Shouldly e NSubstitute, incluídos na `.slnx` — `tests/unit/*/*.csproj`
  Commit: `✅ test: cria projetos de testes unitários com xunit`
- [ ] T002 [H11] Teste de arquitetura das camadas (passa com as dependências permitidas e acusa dependência proibida) — `tests/unit/*/ArquiteturaTests.cs`
  Commit: `✅ test: adiciona teste de arquitetura das camadas`
- [ ] T003 [H12] `coverlet.runsettings` com filtros e exclusões — `coverlet.runsettings`
  Commit: `🔧 chore: adiciona configuração de cobertura com coverlet`
- [ ] T004 [H12] ReportGenerator no manifesto de ferramentas — `.config/dotnet-tools.json`
  Commit: `🔧 chore: adiciona reportgenerator ao manifesto de ferramentas`
- [ ] T005 [H12] Script de verificação da cobertura mínima (`-Minimo`, `-ExecutarTestes`) — `scripts/verificar-cobertura.ps1`
  Commit: `🔧 chore: adiciona script de verificação da cobertura mínima de 90%`

## Fase 2 — Pipeline

- [ ] T006 [H11] Workflow `ci.yml` com gatilhos `push`, `pull_request` e `workflow_dispatch` e job `testes-unitarios` (build, testes `Category=Unit`, cobertura, resumo e artefatos) — `.github/workflows/ci.yml`
  Commit: `👷 ci: adiciona pipeline com testes unitários e cobertura`
- [ ] T007 [H12] Job `testes-integracao` com todos os testes, artefato `cobertura-total` e mínimo de 90% — `.github/workflows/ci.yml`
  Commit: `👷 ci: adiciona testes de integração e cobertura mínima na pipeline`
- [ ] T008 [H11] Badge do status da pipeline e seção "Testes e cobertura" no README — `README.md`
  Commit: `📝 docs: adiciona status da pipeline e cobertura no readme`

## Fase 3 — Validação

- [ ] T009 [H12] Verificar a falha da pipeline com um teste quebrado numa branch de teste (não vai para a `main`) e registrar o resultado no PR
- [ ] T010 [H11] Executar a pipeline manualmente, baixar o artefato e conferir o relatório HTML
- [ ] T011 Ao fim de cada feature 001–008: conferir que os testes novos aparecem no relatório e que há casos de sucesso e de erro

## Dependências

T001 → T002 → T006; T003, T004 → T005 → T007; T006 → T007 → T008 → T009 → T010.
