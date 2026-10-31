# Tarefas — Feature 005 (Doação)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Domínio e contratos (TDD)

- [X] T001 [H06] `PodeReceberDoacao` e `RegistrarDoacaoProcessada` no agregado, com testes (antes do início, no período, depois do fim, encerrada) — `Campanhas.Domain/Campanhas/Campanha.cs`
  Commit: `✨ feat(campanhas): adiciona regras de doação no agregado Campanha`
- [X] T002 [P] [H06] `DoacaoRecebidaEvent` e `DoacaoProcessadaEvent` v1 com testes de snapshot — `ConexaoSolidaria.Contracts/V1/*`, `tests/contract/ConexaoSolidaria.ContractTests/*`
  Commit: `🏷️ feat(contracts): adiciona eventos de doação v1`

## Fase 2 — Recebimento na API

- [X] T003 [H06] Caso de uso `ReceberDoacao` com validador e testes (202, 404, 422, total não alterado) — `Campanhas.Application/Doacoes/*`
  Commit: `✨ feat(campanhas): adiciona caso de uso de recebimento de doação`
- [X] T004 [H06] Tabelas `doacoes_processadas` e `chaves_idempotencia` na migration — `Campanhas.Infrastructure/Persistencia/*`
  Commit: `🗃️ feat(campanhas): adiciona tabelas de idempotência de doações`
- [X] T005 [H06] Endpoint `POST /doacoes` com policy `Doador` e Outbox — `Campanhas.Api/Endpoints/DoacoesEndpoints.cs`
  Commit: `✨ feat(campanhas): adiciona recebimento de doação com outbox`
- [X] T006 [H06] `Idempotency-Key` com hash do corpo e limpeza após 24 h — `Campanhas.Application/Doacoes/ReceberDoacao.cs`, `Campanhas.Infrastructure/Idempotencia/*`
  Commit: `✨ feat(campanhas): adiciona chave de idempotência na doação`

## Fase 3 — Processamento no Worker

- [X] T007 [H07] Projeto `Arrecadacao.Worker` com MassTransit, `ServiceDefaults`, health e `/metrics` na porta 9090 — `Arrecadacao.Worker/*`
  Commit: `🎉 chore(worker): configura worker com masstransit e health checks`
- [X] T008 [H07] `ProcessarDoacao` com `RegistroArrecadacaoSql` (transação: idempotência + `UPDATE` + Outbox), com testes — `Campanhas.Application/Arrecadacao/*`, `Campanhas.Infrastructure/Arrecadacao/RegistroArrecadacaoSql.cs`
  Commit: `✨ feat(worker): soma doação ao total com idempotência`
- [X] T009 [H07] `LivroDoacoesMongo` com upsert e índice, executado também na reentrega — `Campanhas.Infrastructure/Arrecadacao/LivroDoacoesMongo.cs`
  Commit: `✨ feat(worker): adiciona livro de doações no mongodb`
- [X] T010 [H07] Invalidação do cache do painel após processar — `Campanhas.Application/Arrecadacao/ProcessarDoacao.cs`
  Commit: `⚡️ feat(worker): invalida cache do painel após processar doação`
- [X] T011 [H07] `DoacaoRecebidaConsumer` com retentativas (3 imediatas + 5 s, 30 s, 2 min) e fila de erro — `Arrecadacao.Worker/Consumidores/DoacaoRecebidaConsumer.cs`
  Commit: `🥅 feat(worker): configura consumidor com retentativas e fila de erro`
- [X] T012 [P] [H07] Métricas de doação na API e no Worker — `Campanhas.Application/Doacoes/*`, `Campanhas.Application/Arrecadacao/*`
  Commit: `📈 feat(worker): adiciona métricas de doações recebidas e processadas`

## Fase 4 — Testes de integração e aceitação

- [ ] T013 [H06] Testes de integração da API: 202 grava Outbox, 400, 403, 404, 422 sem Outbox, `Idempotency-Key` — `tests/integration/Campanhas.IntegrationTests/DoacoesTests.cs`
  Commit: `✅ test(campanhas): adiciona testes de integração da doação`
- [ ] T014 [H07] Testes do Worker: soma, duplicata, livro na reentrega, falha transitória, falha permanente → `_error`, soma do livro = total — `tests/integration/Arrecadacao.Worker.IntegrationTests/*`
  Commit: `✅ test(worker): adiciona testes de reentrega e falha permanente`
- [ ] T015 [H06] [H07] Cenários Gherkin + cenário "total após doação" da feature 004 — `tests/e2e/ConexaoSolidaria.Bdd/Features/005-doacao.feature`
  Commit: `✅ test(bdd): adiciona cenários de doação e painel atualizado`
- [ ] T016 [P] [H06] Script k6 com 20 VUs e threshold p95 < 500 ms — `tests/e2e/load/doacao.js`
  Commit: `✅ test(load): adiciona teste de carga da doação com k6`

## Fase 5 — Interface

- [ ] T017 [H06] Página `/campanhas/{id}/doar` (`InteractiveServer`) com botão desabilitado durante o envio, `Idempotency-Key` por tentativa e mensagem de processamento, com testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Doar.razor(.cs)`
  Commit: `✨ feat(web): adiciona fluxo de doação`

## Fase 6 — Fechamento

- [ ] T018 Programação em par na revisão do Outbox e do consumidor; `/security-review` e `/code-review`; cobertura ≥ 90%; `git rebase --exec`

## Dependências

T001 → T003 → T005 → T006; T002 → T005, T008; T004 → T005, T008; T007 → T008 → T009 → T010 → T011 → T014; T005 → T013; T011, T013 → T015; T005 → T016, T017.
