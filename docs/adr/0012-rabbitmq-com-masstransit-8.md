# ADR 0012 — RabbitMQ com MassTransit 8.x

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O enunciado permite RabbitMQ ou Kafka. O MassTransit v9 passou a ter licença comercial.

## Decisão

RabbitMQ 4 com plugin de management e Prometheus; MassTransit 8.x (Apache 2.0) fixado no `Directory.Packages.props`.

## Alternativas consideradas

Kafka (mais pesado no Minikube); `RabbitMQ.Client` puro (reimplementar retry, Outbox e DLQ).

## Consequências

Outbox, retry, DLQ e tracing prontos; atualizações limitadas à linha 8.x.

## Referências

- [API e eventos](../../specs/product/api-e-eventos.md)
