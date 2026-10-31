# Testes de integração

Testam cada serviço com a infraestrutura real em containers (Testcontainers). Exigem o Docker ligado. Rodam no hook `pre-push` e no CI.

| Projeto | Cobre | Containers |
|---|---|---|
| `Identity.IntegrationTests` | Endpoints da Identity, migrations, cifragem do CPF, seed | PostgreSQL |
| `Campanhas.IntegrationTests` | Endpoints de Campanhas, Outbox, cache, busca, indexação | PostgreSQL, RabbitMQ, Redis, Elasticsearch |
| `Arrecadacao.Worker.IntegrationTests` | Consumo, idempotência, livro de doações, retentativas e fila de erro | PostgreSQL, RabbitMQ, MongoDB, Redis |

## Regras

- `WebApplicationFactory` para as APIs; MassTransit Test Harness para consumidores.
- Banco limpo por classe de teste (Respawn ou banco por fixture).
- Sem `Thread.Sleep`: esperas com timeout e polling.
- Atributo `[Trait("Category", "Integration")]`.

Estratégia completa: [docs/testes.md](../../docs/testes.md).
