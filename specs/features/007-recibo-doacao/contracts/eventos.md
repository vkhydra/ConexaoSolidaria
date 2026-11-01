# Contrato de eventos — Feature 007 (Recibo de doação)

[← Spec](../spec.md)

## Evento consumido

`DoacaoProcessadaEvent` v1 — definição completa em [005 — eventos](../../005-doacao/contracts/eventos.md).

## Gatilho da função

```csharp
/// <summary>
/// Emite o recibo de uma doação processada, a partir do envelope JSON do MassTransit.
/// </summary>
[Function("EmitirRecibo")]
public async Task ExecutarAsync(
    [RabbitMQTrigger("notificacoes-recibo", ConnectionStringSetting = "RabbitMqConnection")] string envelope,
    FunctionContext contexto)
```

| Item | Valor |
|---|---|
| Fila | `notificacoes-recibo` |
| Conexão | `RabbitMqConnection` (variável de ambiente, vinda de Secret) |
| Formato | Envelope do MassTransit: `{"messageId", "messageType": [...], "headers", "message": {...}}`; `decimal` vem como texto |
| Rastreamento | `traceparent` vai no cabeçalho AMQP (não no envelope); a instrumentação do gatilho continua o trace quando disponível |
| Falha | Exceção → retentativa pelo runtime → `notificacoes-recibo_error` |

## Garantias

- Entrega at-least-once; a função é idempotente por `doacaoId`.
- O consumidor aceita só `messageType` `urn:message:ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent`.
