# 09 — API e eventos

[← Índice](../SPEC.md) · Anterior: [08 — Decisões arquiteturais](08-decisoes-arquiteturais.md) · Próximo: [10 — Dados](10-dados.md)

## 9.1 Convenções da API

| Item | Regra |
|---|---|
| Estilo | REST com Minimal APIs, rotas agrupadas por recurso (`MapGroup`) |
| Base | `https://api.conexao.local/api/v1` (pelo Kong) |
| Recursos | Substantivos no plural, minúsculas, kebab-case |
| Versão | Na URL (`/v1`); mudança incompatível cria `/v2` |
| Formato | JSON, `camelCase`, datas `yyyy-MM-dd`, data-hora ISO 8601 com fuso, valores decimais como número |
| Enums | Como texto (`"Ativa"`), não número |
| Paginação | `page` (≥ 1, padrão 1), `pageSize` (1–100, padrão 20); resposta com `items`, `page`, `pageSize`, `totalItems`, `totalPages` |
| Erros | `application/problem+json` (RFC 9457) com `type`, `title`, `status`, `detail`, `instance`, `code`, `traceId` e, em validação, `errors` |
| Autenticação | `Authorization: Bearer <jwt>` |
| Correlação | `X-Correlation-ID` (gerado pelo Kong se ausente) e `traceparent` (W3C) |
| Documentação | OpenAPI nativo do .NET 10 (`/openapi/v1.json`) + Swagger UI (`/swagger`) só fora de produção |

### Exemplo de erro de validação (400)
```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Dados inválidos",
  "status": 400,
  "code": "validacao",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": {
    "cpf": ["CPF inválido."],
    "senha": ["A senha deve ter ao menos 8 caracteres, com letra, número e caractere especial."]
  }
}
```

### Exemplo de erro de regra (422)
```json
{
  "type": "https://conexao.local/erros/doacao.campanha_nao_ativa",
  "title": "Campanha não recebe doações",
  "status": 422,
  "detail": "A campanha está Cancelada.",
  "code": "doacao.campanha_nao_ativa",
  "traceId": "00-..."
}
```

## 9.2 Identity.Api

### `POST /doadores` — público
```json
// Requisição
{
  "nomeCompleto": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpf": "529.982.247-25",
  "senha": "Senha@123",
  "aceitePoliticaPrivacidade": true
}
// 201 Created — Location: /api/v1/doadores/me
{
  "id": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
  "nomeCompleto": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpfMascarado": "***.982.247-**",
  "criadoEm": "2026-11-10T14:03:22-03:00"
}
```
Erros: 400 (formato), 409 `doador.email_em_uso` / `doador.cpf_em_uso`.

### `POST /auth/login` — público
```json
// Requisição
{ "email": "maria@exemplo.com", "senha": "Senha@123" }
// 200 OK
{ "accessToken": "eyJhbGciOiJSUzI1NiIs...", "tokenType": "Bearer", "expiresIn": 3600, "role": "Doador" }
```
Erros: 400, 401 `auth.credenciais_invalidas`, 429 `auth.bloqueado`.

### `GET /doadores/me` — Doador
200 com o mesmo corpo do cadastro, mais `consentimento: { versao, aceitoEm }`.

### `DELETE /doadores/me` — Doador
204. Erros: 401.

## 9.3 Campanhas.Api

### `POST /campanhas` — GestorONG
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
Erros: 400, 401, 403, 422 `campanha.data_fim_passada` / `campanha.meta_invalida` / `campanha.periodo_invalido`.

### `GET /campanhas?status=Ativa&page=1&pageSize=20` — GestorONG
200 paginado com itens no formato acima.

### `GET /campanhas/{id}` — GestorONG
200 com `ETag`. Erro: 404 `campanha.nao_encontrada`.

### `PUT /campanhas/{id}` — GestorONG
Corpo igual ao `POST`; cabeçalho `If-Match: "<versão>"` obrigatório. 204. Erros: 404, 409 `campanha.conflito_de_versao`, 412 sem `If-Match`, 422 `campanha.nao_editavel`.

### `PATCH /campanhas/{id}/status` — GestorONG
```json
{ "status": "Cancelada" }
```
204. Erros: 404, 422 `campanha.transicao_invalida`.

### `GET /transparencia/campanhas` — público
```json
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
Cabeçalhos: `Cache-Control: public, max-age=10`. Sem paginação (limite de 100 campanhas ativas, ordenadas por `dataFim`).

### `GET /campanhas/busca?q=natl%20solidaro&page=1&pageSize=10` — público
```json
{
  "items": [
    { "id": "0192f3b0-...", "titulo": "Natal Solidário", "trecho": "Presentes e <em>ceia</em>...", "relevancia": 3.42, "dataFim": "2026-12-24" }
  ],
  "page": 1, "pageSize": 10, "totalItems": 1, "totalPages": 1
}
```
Erros: 400 (`q` fora de 2–100 caracteres), 503 `busca.indisponivel`.

### `POST /doacoes` — Doador
```json
// Requisição (cabeçalho opcional Idempotency-Key: 6f1c...)
{ "idCampanha": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f", "valorDoacao": 150.00 }
// 202 Accepted
{ "doacaoId": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f", "status": "Recebida", "mensagem": "Doação recebida. O total será atualizado em instantes." }
```
Erros: 400, 401, 403, 404 `campanha.nao_encontrada`, 422 `doacao.campanha_nao_ativa`.

### `POST /admin/reindexar` — GestorONG
202. Reindexa todas as campanhas no Elasticsearch.

## 9.4 Endpoints técnicos (todos os serviços)

| Rota | Conteúdo | Exposto no Kong |
|---|---|---|
| `/health/live` | Processo vivo (sem checar dependências) | Não |
| `/health/ready` | Dependências (banco, broker, cache) em JSON | Não |
| `/metrics` | Formato Prometheus | Não (coletado dentro do cluster) |
| `/swagger`, `/openapi/v1.json` | Documentação | Sim, só no overlay local |

## 9.5 Eventos de integração (contratos v1)

Projeto `BuildingBlocks.Contracts`, namespace `ConexaoSolidaria.Contracts.V1`. Contratos são `record` imutáveis.

```csharp
public sealed record DoacaoRecebidaEvent(
    Guid DoacaoId, Guid CampanhaId, Guid DoadorId, decimal Valor, DateTimeOffset OcorridoEm);

public sealed record DoacaoProcessadaEvent(
    Guid DoacaoId, Guid CampanhaId, Guid DoadorId, decimal Valor,
    decimal TotalArrecadado, DateTimeOffset RecebidaEm, DateTimeOffset ProcessadaEm);

public sealed record CampanhaAlteradaEvent(
    Guid CampanhaId, string Titulo, string Descricao, string Status,
    decimal MetaFinanceira, DateOnly DataFim, long Versao);
```

### Regras de evolução
1. Só adicionar campos opcionais no fim; nunca remover ou renomear.
2. Mudança incompatível cria `V2` com nome novo, e as duas versões convivem até todos os consumidores migrarem.
3. Sem dados pessoais: só IDs.
4. Teste de contrato: serialização JSON de cada evento comparada com um snapshot versionado em `tests/` (quebra o build se o contrato mudar sem intenção).

### Topologia no RabbitMQ (MassTransit)

| Exchange (fanout, durável) | Fila | Consumidor | DLQ |
|---|---|---|---|
| `ConexaoSolidaria.Contracts.V1:DoacaoRecebidaEvent` | `doacao-recebida` | `Arrecadacao.Worker` | `doacao-recebida_error` |
| `ConexaoSolidaria.Contracts.V1:DoacaoProcessadaEvent` | `notificacoes-recibo` | `Notificacoes.Function` | `notificacoes-recibo_error` |
| `ConexaoSolidaria.Contracts.V1:CampanhaAlteradaEvent` | `indexar-campanha` | `Campanhas.Api` | `indexar-campanha_error` |

- Filas criadas pelo MassTransit, exceto `notificacoes-recibo`, que é declarada com o binding em `deploy/rabbitmq/definitions.json` (a função não usa MassTransit).
- A função lê o envelope JSON do MassTransit (`{"messageId", "headers", "message": {...}}`) e desserializa só o campo `message`.
- Mensagens persistentes; `prefetch` 16 no Worker.

### Exemplo de mensagem no broker
```json
{
  "messageId": "0192f3c4-0000-7000-8000-000000000001",
  "correlationId": "6f1c...",
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

## 9.6 Coleção Postman

`docs/postman/conexao-solidaria.postman_collection.json` + ambiente `local`:

1. Pastas por fluxo: **Setup** (login do gestor), **Campanhas**, **Doador**, **Doação**, **Painel e busca**, **Erros esperados**.
2. Scripts de teste salvam o token e os IDs em variáveis (`{{tokenGestor}}`, `{{tokenDoador}}`, `{{campanhaId}}`).
3. Cada requisição tem um teste de status esperado; a coleção roda no Collection Runner como teste de fumaça.
