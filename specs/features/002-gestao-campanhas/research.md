# Pesquisa — Feature 002 (Gestão de campanhas)

[← Spec](spec.md)

## R1 — Concorrência otimista

- **Decisão:** coluna de sistema `xmin` do PostgreSQL como token de concorrência (`IsRowVersion()` no EF Core), exposta como `ETag` e exigida em `If-Match`.
- **Justificativa:** sem coluna extra; o EF Core lança `DbUpdateConcurrencyException`, convertida em 409.
- **Alternativas:** coluna `versao` incrementada à mão (mais código); "último a salvar vence" (perde alterações).
- **Fonte:** MCP Microsoft Learn, "Concurrency tokens with Npgsql".

## R2 — Data de referência ("hoje")

- **Decisão:** `TimeProvider` injetado e convertido para `America/Sao_Paulo` (`TimeZoneInfo.FindSystemTimeZoneById`), resultando em `DateOnly`.
- **Justificativa:** testes controlam a data com `FakeTimeProvider`; o fuso funciona em Linux e Windows com .NET 10 (IDs IANA).

## R3 — Publicação do evento de alteração

- **Decisão:** eventos de domínio (`CampanhaCriada`, `CampanhaAtualizada`, `CampanhaConcluida`, `CampanhaCancelada`) convertidos em `CampanhaAlteradaEvent` e gravados no Outbox do MassTransit na mesma transação.
- **Justificativa:** a busca nunca fica sem o evento, mesmo se o RabbitMQ estiver fora ([ADR 0005](../../../docs/adr/0005-doacao-assincrona-com-outbox.md)).
- **Alternativas:** publicar direto no broker após o `SaveChanges` (pode perder o evento).

## R4 — Representação de dinheiro

- **Decisão:** `decimal` com 2 casas no VO `Dinheiro` e `numeric(18,2)` no banco.
- **Justificativa:** sem erro de arredondamento; `double` é proibido para valores monetários.

## R5 — Mapeamento de erros

- **Decisão:** `Result` do domínio → `ProblemDetails` 422 com `code`; validação de formato (FluentValidation) → 400 com `errors`.
- **Justificativa:** distingue "pedido mal formado" de "regra de negócio violada" ([API e eventos](../../product/api-e-eventos.md)).
