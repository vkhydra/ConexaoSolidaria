# Modelo de dados — Feature 007 (Recibo de doação)

[← Spec](spec.md)

## Entrada: `DoacaoProcessadaEvent` (v1)

Contrato em [005 — eventos](../005-doacao/contracts/eventos.md). A função lê só o campo `message` do envelope:

| Campo | Uso no recibo |
|---|---|
| `doacaoId` | Identificador do recibo e chave de deduplicação |
| `campanhaId` | Campanha beneficiada |
| `doadorId` | Referência pseudônima ao doador (sem consulta à Identity) |
| `valor` | Valor doado |
| `totalArrecadado` | Total da campanha após a doação |
| `processadaEm` | Data do recibo |

## Recibo (log de negócio)

```json
{
  "@t": "2026-11-10T17:10:07.123Z",
  "@mt": "ReciboEmitido para a doação {DoacaoId} da campanha {CampanhaId} no valor de {Valor}",
  "EventId": { "Name": "ReciboEmitido" },
  "DoacaoId": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f",
  "CampanhaId": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "DoadorId": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
  "Valor": 150.00,
  "TraceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Proibido no log: nome, e-mail, CPF.

## Deduplicação (Redis)

| Chave | Valor | Expiração |
|---|---|---|
| `conexao:recibos:v1:<doacaoId>` | `1` | 7 dias (`EX 604800`), criada com `NX` |

## Fila (RabbitMQ, `definitions.json`)

| Item | Valor |
|---|---|
| Fila | `notificacoes-recibo` (durável) |
| Binding | Exchange `ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent` (fanout) |
| DLQ | `notificacoes-recibo_error` via `x-dead-letter-exchange` |
