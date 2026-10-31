# ADR 0005 — Doação assíncrona com Transactional Outbox

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

Publicar no broker e gravar no banco em passos separados pode perder eventos (falha entre os dois).

## Decisão

A API grava o evento na tabela de Outbox do MassTransit na mesma transação e responde 202. Um serviço em segundo plano entrega o evento ao RabbitMQ.

## Alternativas consideradas

Publicar direto no RabbitMQ (risco de perda); two-phase commit (não suportado e caro).

## Consequências

Entrega *at-least-once*; consumidores precisam ser idempotentes (ADR 0006); pequeno atraso na entrega (polling do Outbox, ~1 s).

## Referências

- [Feature 005 — Doação](../../specs/features/005-doacao/plan.md)
- [API e eventos](../../specs/product/api-e-eventos.md)
