# API e eventos

[← README](../../README.md)

## Convenções da API

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

## Mapa de endpoints

Os exemplos de requisição e resposta e a tabela de erros de cada endpoint ficam no contrato da feature correspondente.

| Método e rota | Serviço | Acesso | Contrato |
|---|---|---|---|
| `POST /auth/login` | Identity.Api | Público | [001 — Autenticação](../features/001-autenticacao/contracts/api.md) |
| `POST /doadores` | Identity.Api | Público | [003 — Cadastro de doador](../features/003-cadastro-doador/contracts/api.md) |
| `GET /doadores/me` | Identity.Api | `Doador` | [008 — Direitos do titular](../features/008-direitos-titular/contracts/api.md) |
| `DELETE /doadores/me` | Identity.Api | `Doador` | [008 — Direitos do titular](../features/008-direitos-titular/contracts/api.md) |
| `POST /campanhas` | Campanhas.Api | `GestorONG` | [002 — Gestão de campanhas](../features/002-gestao-campanhas/contracts/api.md) |
| `GET /campanhas` | Campanhas.Api | `GestorONG` | [002 — Gestão de campanhas](../features/002-gestao-campanhas/contracts/api.md) |
| `GET /campanhas/{id}` | Campanhas.Api | `GestorONG` | [002 — Gestão de campanhas](../features/002-gestao-campanhas/contracts/api.md) |
| `PUT /campanhas/{id}` | Campanhas.Api | `GestorONG` | [002 — Gestão de campanhas](../features/002-gestao-campanhas/contracts/api.md) |
| `PATCH /campanhas/{id}/status` | Campanhas.Api | `GestorONG` | [002 — Gestão de campanhas](../features/002-gestao-campanhas/contracts/api.md) |
| `GET /transparencia/campanhas` | Campanhas.Api | Público | [004 — Painel de transparência](../features/004-painel-transparencia/contracts/api.md) |
| `POST /doacoes` | Campanhas.Api | `Doador` | [005 — Doação](../features/005-doacao/contracts/api.md) |
| `GET /campanhas/busca` | Campanhas.Api | Público | [006 — Busca de campanhas](../features/006-busca-campanhas/contracts/api.md) |
| `POST /admin/reindexar` | Campanhas.Api | `GestorONG` | [006 — Busca de campanhas](../features/006-busca-campanhas/contracts/api.md) |

Todas as rotas acima ficam sob a base `https://api.conexao.local/api/v1`.

## Endpoints técnicos (todos os serviços)

| Rota | Conteúdo | Exposto no Kong |
|---|---|---|
| `/health/live` | Processo vivo (sem checar dependências) | Não |
| `/health/ready` | Dependências (banco, broker, cache) em JSON | Não |
| `/metrics` | Formato Prometheus | Não (coletado dentro do cluster) |
| `/swagger`, `/openapi/v1.json` | Documentação | Sim, só no overlay local |

## Eventos de integração (contratos v1)

Detalhes de produtor, consumidor e exemplos: [eventos da doação](../features/005-doacao/contracts/eventos.md), [eventos da campanha](../features/002-gestao-campanhas/contracts/eventos.md) e [gatilho do recibo](../features/007-recibo-doacao/contracts/eventos.md).

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
4. Teste de contrato: serialização JSON de cada evento comparada com um snapshot versionado em `tests/contract/` (quebra o build se o contrato mudar sem intenção).

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

## Coleção Postman

`docs/postman/conexao-solidaria.postman_collection.json` + ambiente `local`:

1. Pastas por fluxo: **Setup** (login do gestor), **Campanhas**, **Doador**, **Doação**, **Painel e busca**, **Erros esperados**.
2. Scripts de teste salvam o token e os IDs em variáveis (`{{tokenGestor}}`, `{{tokenDoador}}`, `{{campanhaId}}`).
3. Cada requisição tem um teste de status esperado; a coleção roda no Collection Runner como teste de fumaça.
