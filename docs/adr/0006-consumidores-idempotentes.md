# ADR 0006 — Consumidores idempotentes

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

At-least-once implica mensagens duplicadas.

## Decisão

Tabela `doacoes_processadas` com PK `doacao_id`, inserida na mesma transação do `UPDATE` do total. Duplicata → violação de PK → mensagem confirmada sem efeito. MongoDB com upsert por `_id = doacaoId`. Função com deduplicação por `doacaoId`.

## Alternativas consideradas

Inbox do MassTransit (deduplica por `MessageId`, não por regra de negócio; usado como camada extra).

## Consequências

Reentregas são seguras; o teste "evento duplicado" é obrigatório.

## Referências

- [Feature 005 — Doação](../../specs/features/005-doacao/data-model.md)
