# Contrato de eventos — Feature 002 (Gestão de campanhas)

[← Spec](../spec.md)

## `CampanhaAlteradaEvent` (v1)

Namespace `ConexaoSolidaria.Contracts.V1`, projeto `ConexaoSolidaria.Contracts`.

```csharp
/// <summary>
/// Publicado sempre que uma campanha é criada ou alterada. Alimenta o índice de busca.
/// </summary>
public sealed record CampanhaAlteradaEvent(
    Guid CampanhaId, string Titulo, string Descricao, string Status,
    decimal MetaFinanceira, DateOnly DataFim, long Versao);
```

| Item | Valor |
|---|---|
| Produtor | `Campanhas.Api` (via Outbox, na transação da alteração) |
| Exchange | `ConexaoSolidaria.Contracts.V1:CampanhaAlteradaEvent` (fanout, durável) |
| Fila do consumidor | `indexar-campanha` → `IndexarCampanhaConsumer` ([006](../../006-busca-campanhas/plan.md)) |
| DLQ | `indexar-campanha_error` |
| Ordenação | `Versao` crescente; o consumidor ignora versões antigas |
| Dados pessoais | Nenhum |

Exemplo do campo `message` no envelope do MassTransit:
```json
{
  "campanhaId": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "titulo": "Natal Solidário",
  "descricao": "Presentes e ceia para 120 crianças.",
  "status": "Ativa",
  "metaFinanceira": 10000.00,
  "dataFim": "2026-12-24",
  "versao": 1
}
```

Regras de evolução e topologia: [API e eventos](../../../product/api-e-eventos.md).
