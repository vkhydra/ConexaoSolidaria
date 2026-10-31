# Contrato HTTP — Feature 005 (Doação)

[← Spec](../spec.md)

## `POST /doacoes` — `Doador`

```json
// Requisição (cabeçalho opcional Idempotency-Key: 6f1c2d3e-...)
{ "idCampanha": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", "valorDoacao": 150.00 }

// 202 Accepted
{
  "doacaoId": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f",
  "status": "Recebida",
  "mensagem": "Doação recebida. O total será atualizado em instantes."
}
```

| Status | Código | Quando |
|---|---|---|
| 400 | `validacao` | Valor ≤ 0, mais de 2 casas, acima de 1.000.000,00 ou ID mal formado |
| 401 | — | Sem token ou token inválido |
| 403 | — | Token sem role `Doador` |
| 404 | `campanha.nao_encontrada` | Campanha inexistente |
| 422 | `doacao.campanha_nao_ativa` | Campanha não apta (status ou período) |
| 422 | `doacao.chave_idempotencia_conflitante` | Mesma `Idempotency-Key` com corpo diferente |
| 429 | — | Limite do Kong (60/min por IP) |

## `POST /admin/doacoes/reprocessar` — `GestorONG` (operação)

Não existe: o reenvio de mensagens da fila de erro é feito pelo painel do RabbitMQ, seguindo o [runbook](../../../../docs/runbooks/mensagens-na-dlq.md).

## Eventos

[Contrato de eventos](eventos.md): `DoacaoRecebidaEvent` (API → Worker) e `DoacaoProcessadaEvent` (Worker → função de recibo).
