# Tarefas — Feature 007 (Recibo de doação)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 0 — Spike (time-box 1 dia)

- [X] T001 [H09] Spike: gatilho RabbitMQ no isolated worker lendo o envelope do MassTransit; registrar o resultado em [research.md](research.md) (R2 e R4). Código do spike não vai para a `main`.
  Commit: `📝 docs(specs): registra resultado do spike da função de recibo`

## Fase 1 — Testes e núcleo (TDD)

- [X] T002 [H09] `LeitorEnvelopeMassTransit` com testes (envelope válido, tipo errado, JSON inválido) — `Notificacoes.Function/Recibos/LeitorEnvelopeMassTransit.cs`
  Commit: `✨ feat(notificacoes): adiciona leitor do envelope do masstransit`
- [X] T003 [H09] `EmissorRecibo` com teste do conteúdo do log (sem dados pessoais) — `Notificacoes.Function/Recibos/EmissorRecibo.cs`
  Commit: `✨ feat(notificacoes): adiciona emissor de recibo em log estruturado`
- [X] T004 [H09] `IDeduplicadorRecibos` e `DeduplicadorRecibosRedis` com teste de integração — `Notificacoes.Function/Recibos/*`
  Commit: `✨ feat(notificacoes): adiciona deduplicação de recibos no redis`

## Fase 2 — Função

- [X] T005 [H09] `Program.cs`, `host.json` e função `EmitirRecibo` com gatilho RabbitMQ e OpenTelemetry — `Notificacoes.Function/*`
  Commit: `✨ feat(notificacoes): adiciona função de recibo com gatilho rabbitmq`
- [ ] T006 [H09] Fila `notificacoes-recibo`, binding e DLQ em `definitions.json` — `deploy/rabbitmq/definitions.json`
  Commit: `🔧 chore(notificacoes): adiciona definição da fila de recibos`

## Fase 3 — Empacotamento e escala

- [ ] T007 [H09] Dockerfile da função — `src/Functions/Notificacoes.Function/Dockerfile`
  Commit: `🧱 build(notificacoes): adiciona dockerfile da função`
- [ ] T008 [H09] Deployment com 0 réplicas, `ScaledObject` (0–3, fila > 10, cooldown 60 s) e `TriggerAuthentication` — `deploy/k8s/base/apps/notificacoes-function/*`
  Commit: `🧱 build(k8s): adiciona deployment e scaledobject da função de recibo`

## Fase 4 — Aceitação

- [ ] T009 [H09] Cenário Gherkin do recibo (captura de log no ambiente de teste) — `tests/e2e/ConexaoSolidaria.Bdd/Features/007-recibo-doacao.feature`
  Commit: `✅ test(bdd): adiciona cenário de recibo de doação`

## Fase 5 — Fechamento

- [ ] T010 `/code-review`; cobertura ≥ 90%; validar escala 0 → 1 → 0 no Minikube; `git rebase --exec`

## Dependências

T001 → T002 → T005; T003, T004 → T005 → T006 → T009; T005 → T007 → T008.
