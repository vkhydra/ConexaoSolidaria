# Contrato HTTP — Feature 006 (Busca de campanhas)

[← Spec](../spec.md)

## `GET /campanhas/busca?q=natl%20solidaro&page=1&pageSize=10` — público

```json
// 200 OK
{
  "items": [
    {
      "id": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
      "titulo": "Natal Solidário",
      "trecho": "Presentes e <em>ceia</em> para 120 crianças.",
      "relevancia": 3.42,
      "dataFim": "2026-12-24"
    }
  ],
  "page": 1, "pageSize": 10, "totalItems": 1, "totalPages": 1
}
```

| Status | Código | Quando |
|---|---|---|
| 400 | `validacao` | `q` fora de 2–100 caracteres; `page` < 1; `pageSize` fora de 1–50 |
| 429 | — | Limite do Kong (60/min por IP) |
| 503 | `busca.indisponivel` | Elasticsearch fora do ar |

O `trecho` vem do destaque do Elasticsearch com `<em>`; a interface o exibe como HTML sanitizado (só a tag `em`).

## `POST /admin/reindexar` — `GestorONG`

202 Accepted. Recria o índice a partir do PostgreSQL e troca o alias `campanhas` ao final. Erros: 401, 403, 409 `busca.reindexacao_em_andamento`.

## Evento consumido

`CampanhaAlteradaEvent` v1, fila `indexar-campanha`: [contrato](../../002-gestao-campanhas/contracts/eventos.md).
