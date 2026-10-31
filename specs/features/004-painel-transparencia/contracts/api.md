# Contrato HTTP — Feature 004 (Painel de transparência)

[← Spec](../spec.md)

## `GET /transparencia/campanhas` — público

```json
// 200 OK — Cache-Control: public, max-age=10
[
  {
    "id": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
    "titulo": "Natal Solidário",
    "metaFinanceira": 10000.00,
    "totalArrecadado": 150.00,
    "percentualAtingido": 1.50,
    "dataFim": "2026-12-24"
  }
]
```

| Status | Quando |
|---|---|
| 200 | Sempre que o banco responde (lista pode ser vazia) |
| 429 | Limite do Kong (60/min por IP) |
| 503 | Banco indisponível (`ProblemDetails` com `code` `servico.indisponivel`) |

- Sem paginação: no máximo 100 campanhas, ordenadas por `dataFim`.
- Sem autenticação; o cabeçalho `Authorization`, se enviado, é ignorado.
