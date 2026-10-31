# ADR 0012 — RabbitMQ com MassTransit 8.x

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A Fase 2 ensina RabbitMQ e Kafka. O MassTransit v9 passou a ter licença comercial.

## Decisão

RabbitMQ 4 com plugin de management e Prometheus; MassTransit 8.x (Apache 2.0) fixado no `Directory.Packages.props`.

## Alternativas consideradas

Kafka (mais pesado no Minikube); `RabbitMQ.Client` puro (reimplementar retry, Outbox e DLQ).

## Consequências

Outbox, retry, DLQ e tracing prontos; atualizações limitadas à linha 8.x.

## Referências

- [API e eventos](../../specs/product/api-e-eventos.md)
