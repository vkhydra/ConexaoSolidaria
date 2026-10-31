# Pesquisa — Feature 005 (Doação)

[← Spec](spec.md)

## R1 — Publicação confiável do evento

- **Decisão:** Transactional Outbox do MassTransit (EF Core): o evento é gravado em `outbox_message` na transação; um serviço em segundo plano entrega ao RabbitMQ.
- **Justificativa:** sem perda de evento se o broker cair entre o banco e a publicação.
- **Alternativas:** publicar direto (risco de perda); two-phase commit (não suportado).
- **Registro:** [ADR 0005](../../../docs/adr/0005-doacao-assincrona-com-outbox.md).

## R2 — Idempotência no Worker

- **Decisão:** `INSERT` em `doacoes_processadas` (PK `doacao_id`) na mesma transação do `UPDATE` do total. PK duplicada → doação já processada → confirma a mensagem sem somar.
- **Justificativa:** regra de negócio (uma doação, uma soma), não só deduplicação técnica.
- **Alternativas:** só o Inbox do MassTransit (deduplica por `MessageId`, que muda se a API republicar).
- **Registro:** [ADR 0006](../../../docs/adr/0006-consumidores-idempotentes.md).

## R3 — Atualização do total

- **Decisão:** `UPDATE campanhas SET total_arrecadado = total_arrecadado + @Valor ... RETURNING total_arrecadado` (Dapper ou `ExecuteSqlInterpolated`), sem carregar o agregado.
- **Justificativa:** atômico no banco; sem conflito de concorrência com edições do gestor (que usam `xmin`).
- **Observação:** a regra está documentada e testada em `Campanha.RegistrarDoacaoProcessada`.

## R4 — Livro de doações fora da transação

- **Decisão:** gravar no MongoDB depois do `COMMIT` com `ReplaceOne(upsert)` por `_id = doacaoId`; em reentrega o Worker sempre executa o passo, mesmo quando o total já foi somado.
- **Justificativa:** não há transação distribuída PostgreSQL + MongoDB; o upsert idempotente garante o livro completo.

## R5 — Chave de idempotência da API

- **Decisão:** tabela `chaves_idempotencia` (`chave` + `doador_id`) com o `doacao_id` e o hash do corpo; expira em 24 h (tarefa agendada).
- **Justificativa:** clique duplo ou repetição do cliente não gera duas doações.

## R6 — Política de retentativa

- **Decisão:** `UseMessageRetry(r => r.Immediate(3))` + `UseDelayedRedelivery(r => r.Intervals(5s, 30s, 2min))`; depois, fila `_error`.
- **Justificativa:** falhas transitórias (banco reiniciando) se resolvem; falhas permanentes ficam visíveis (alerta `MensagensNaDLQ`).
- **Requisito:** plugin de mensagens atrasadas do RabbitMQ ou redelivery agendado pelo MassTransit.
