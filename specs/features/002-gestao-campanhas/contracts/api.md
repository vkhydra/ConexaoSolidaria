# Contrato HTTP — Feature 002 (Gestão de campanhas)

[← Spec](../spec.md)

Base: `https://api.conexao.local/api/v1`. Todas as rotas exigem a policy `GestorONG`.

## `POST /campanhas`

```json
// Requisição
{
  "titulo": "Natal Solidário",
  "descricao": "Presentes e ceia para 120 crianças.",
  "dataInicio": "2026-11-10",
  "dataFim": "2026-12-24",
  "metaFinanceira": 10000.00
}
// 201 Created — Location: /api/v1/campanhas/0192f3b0-... — ETag: "1"
{
  "id": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "titulo": "Natal Solidário",
  "descricao": "Presentes e ceia para 120 crianças.",
  "dataInicio": "2026-11-10",
  "dataFim": "2026-12-24",
  "metaFinanceira": 10000.00,
  "totalArrecadado": 0.00,
  "percentualAtingido": 0.00,
  "status": "Ativa"
}
```
Erros: 400 `validacao`, 401, 403, 422 `campanha.data_fim_passada` / `campanha.meta_invalida` / `campanha.periodo_invalido`.

## `GET /campanhas?status=Ativa&page=1&pageSize=20`

200 paginado:
```json
{ "items": [ { "...": "formato do POST" } ], "page": 1, "pageSize": 20, "totalItems": 1, "totalPages": 1 }
```
Erros: 400 (`page`/`pageSize` fora do intervalo), 401, 403.

## `GET /campanhas/{id}`

200 com o formato do `POST` e cabeçalho `ETag`. Erros: 401, 403, 404 `campanha.nao_encontrada`.

## `PUT /campanhas/{id}`

Corpo igual ao `POST`; cabeçalho `If-Match: "<versão>"` obrigatório. Resposta 204 com o novo `ETag`.
Erros: 400, 401, 403, 404, 409 `campanha.conflito_de_versao`, 412 `campanha.versao_obrigatoria`, 422 `campanha.nao_editavel` e regras da criação.

## `PATCH /campanhas/{id}/status`

```json
{ "status": "Cancelada" }
```
204. Erros: 400 (status desconhecido), 401, 403, 404, 422 `campanha.transicao_invalida`.

## Evento publicado

`CampanhaAlteradaEvent` v1 em toda criação e alteração: [contrato de eventos](eventos.md).
