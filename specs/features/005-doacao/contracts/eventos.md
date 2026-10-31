# Contrato de eventos — Feature 005 (Doação)

[← Spec](../spec.md)

Namespace `ConexaoSolidaria.Contracts.V1`. Regras de evolução e topologia: [API e eventos](../../../product/api-e-eventos.md).

## `DoacaoRecebidaEvent` (v1)

```csharp
/// <summary>
/// Publicado pela Campanhas.Api quando uma doação é aceita. Ainda não somada ao total.
/// </summary>
public sealed record DoacaoRecebidaEvent(
    Guid DoacaoId, Guid CampanhaId, Guid DoadorId, decimal Valor, DateTimeOffset OcorridoEm);
```

| Item | Valor |
|---|---|
| Produtor | `Campanhas.Api` (Outbox) |
| Exchange | `ConexaoSolidaria.Contracts.V1:DoacaoRecebidaEvent` (fanout, durável) |
| Fila / consumidor | `doacao-recebida` → `Arrecadacao.Worker` (`prefetch` 16) |
| DLQ | `doacao-recebida_error` |

## `DoacaoProcessadaEvent` (v1)

```csharp
/// <summary>
/// Publicado pelo Arrecadacao.Worker depois de somar a doação ao total da campanha.
/// </summary>
public sealed record DoacaoProcessadaEvent(
    Guid DoacaoId, Guid CampanhaId, Guid DoadorId, decimal Valor,
    decimal TotalArrecadado, DateTimeOffset RecebidaEm, DateTimeOffset ProcessadaEm);
```

| Item | Valor |
|---|---|
| Produtor | `Arrecadacao.Worker` (Outbox) |
| Exchange | `ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent` (fanout, durável) |
| Fila / consumidor | `notificacoes-recibo` → `Notificacoes.Function` ([007](../../007-recibo-doacao/spec.md)) |
| DLQ | `notificacoes-recibo_error` |

## Exemplo de mensagem no broker

```json
{
  "messageId": "0192f3c4-0000-7000-8000-000000000001",
  "messageType": ["urn:message:ConexaoSolidaria.Contracts.V1:DoacaoRecebidaEvent"],
  "headers": { "traceparent": "00-4bf9...-00f0...-01" },
  "message": {
    "doacaoId": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f",
    "campanhaId": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
    "doadorId": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
    "valor": 150.00,
    "ocorridoEm": "2026-11-10T14:10:05-03:00"
  }
}
```

Os dois eventos têm teste de snapshot JSON em `tests/contract/ConexaoSolidaria.ContractTests/`.
