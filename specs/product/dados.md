# 10 — Dados

[← README](../../README.md) · Anterior: [09 — API e eventos](api-e-eventos.md) · Próximo: [11 — Segurança e LGPD](../../docs/spec/11-seguranca-e-lgpd.md)

Esta seção é a base do **PDF de justificativa dos bancos** exigido pelo enunciado (Etapa 17). Referências: `wiki:disciplinas/efcore`, `wiki:disciplinas/nosql`, `wiki:disciplinas/elasticsearch`, `wiki:conceitos/persistencia-poliglota`.

## 10.1 Visão geral

| Banco | Versão | Dono | Papel | Modelo de consistência |
|---|---|---|---|---|
| PostgreSQL | 17 | Identity | Usuários | ACID |
| PostgreSQL | 17 | Campanhas | Campanhas, total, idempotência, Outbox | ACID |
| MongoDB | 8 | Campanhas (Worker) | Livro de doações (audit log) | Forte no primário (instância única local) |
| Redis | 8 | Campanhas | Cache do painel | Cache descartável |
| Elasticsearch | 9 | Campanhas | Índice de busca | Quase em tempo real (refresh ~1 s) |

Local: uma instância PostgreSQL com dois bancos e usuários separados (`identity_app`, `campanhas_app`), cada um sem acesso ao banco do outro.

## 10.2 Justificativa (resumo do PDF)

| Necessidade | Por que esse banco | Por que não os outros |
|---|---|---|
| Valores financeiros e unicidade de e-mail/CPF | PostgreSQL: transações ACID, `numeric(18,2)` exato, constraints únicas, Outbox na mesma transação | NoSQL não oferece a mesma garantia transacional com Outbox no EF Core |
| Histórico imutável de doações | MongoDB: append-only, documento autocontido, escrita rápida, agregações por campanha | No PostgreSQL competiria com a carga transacional; no Elasticsearch não é fonte da verdade |
| Painel público muito lido | Redis: leitura em memória (< 1 ms), TTL, remoção pontual pelo Worker | Consultar o banco a cada acesso aumenta latência e carga |
| Busca com erro de digitação e relevância | Elasticsearch: índice invertido, analyzer em português, fuzzy, `_score` | `LIKE`/`ILIKE` não tem tolerância a erro nem relevância |

## 10.3 PostgreSQL — `identity_db`

Tabela `usuarios`:

| Coluna | Tipo | Restrições |
|---|---|---|
| `id` | `uuid` | PK |
| `nome_completo` | `varchar(150)` | NOT NULL |
| `email` | `varchar(254)` | NOT NULL, UNIQUE (já normalizado) |
| `cpf_cifrado` | `bytea` | NULL (gestor e anonimizado) |
| `cpf_hash` | `bytea` | NULL, UNIQUE parcial (`WHERE cpf_hash IS NOT NULL`) |
| `senha_hash` | `varchar(100)` | NULL após anonimizar |
| `role` | `varchar(20)` | NOT NULL, CHECK (`GestorONG`, `Doador`) |
| `consentimento_versao` | `varchar(20)` | NULL para gestor |
| `consentimento_em` | `timestamptz` | NULL para gestor |
| `criado_em` | `timestamptz` | NOT NULL |
| `anonimizado_em` | `timestamptz` | NULL |

Tabela `tentativas_login` (bloqueio por força bruta): `email_hash` (PK), `falhas`, `bloqueado_ate`.

## 10.4 PostgreSQL — `campanhas_db`

Tabela `campanhas`:

| Coluna | Tipo | Restrições |
|---|---|---|
| `id` | `uuid` | PK |
| `titulo` | `varchar(120)` | NOT NULL |
| `descricao` | `varchar(2000)` | NOT NULL |
| `data_inicio` | `date` | NOT NULL |
| `data_fim` | `date` | NOT NULL, CHECK (`data_fim >= data_inicio`) |
| `meta_financeira` | `numeric(18,2)` | NOT NULL, CHECK (`> 0`) |
| `total_arrecadado` | `numeric(18,2)` | NOT NULL, DEFAULT 0, CHECK (`>= 0`) |
| `status` | `varchar(20)` | NOT NULL, CHECK (`Ativa`, `Concluida`, `Cancelada`) |
| `criado_em`, `atualizado_em` | `timestamptz` | NOT NULL |
| `xmin` | sistema | Token de concorrência (EF Core `IsRowVersion`) |

Índice: `ix_campanhas_status_data_fim (status, data_fim)` para o painel e a listagem.

Tabela `doacoes_processadas` (idempotência do Worker):

| Coluna | Tipo | Restrições |
|---|---|---|
| `doacao_id` | `uuid` | PK |
| `campanha_id` | `uuid` | NOT NULL, FK → `campanhas.id` |
| `valor` | `numeric(18,2)` | NOT NULL, CHECK (`> 0`) |
| `processada_em` | `timestamptz` | NOT NULL |

Tabela `chaves_idempotencia` (cabeçalho `Idempotency-Key` da API): `chave` + `doador_id` (PK composta), `doacao_id`, `criada_em`; registros com mais de 24 h removidos por tarefa agendada.

Tabelas do MassTransit: `inbox_state`, `outbox_message`, `outbox_state` (criadas por migration via `AddInboxStateEntity`, `AddOutboxMessageEntity`, `AddOutboxStateEntity`).

### Operação do Worker (uma transação)
```sql
INSERT INTO doacoes_processadas (doacao_id, campanha_id, valor, processada_em)
VALUES (@DoacaoId, @CampanhaId, @Valor, @Agora);          -- PK duplicada => já processada

UPDATE campanhas
   SET total_arrecadado = total_arrecadado + @Valor, atualizado_em = @Agora
 WHERE id = @CampanhaId
RETURNING total_arrecadado;                                -- vai no DoacaoProcessadaEvent

-- + linha do Outbox com DoacaoProcessadaEvent (MassTransit)
```

### Consulta do painel (Dapper)
```sql
SELECT id, titulo, meta_financeira AS MetaFinanceira, total_arrecadado AS TotalArrecadado, data_fim AS DataFim
  FROM campanhas
 WHERE status = 'Ativa' AND data_fim >= @Hoje
 ORDER BY data_fim
 LIMIT 100;
```

### Migrations
1. Code First com Fluent API (`IEntityTypeConfiguration<T>`), nomes em `snake_case` (`EFCore.NamingConventions`).
2. Uma pasta de migrations por `DbContext` (`IdentityDbContext`, `CampanhasDbContext`).
3. Aplicadas por **migration bundle** (`dotnet ef migrations bundle`) executado num `Job` do Kubernetes antes do rollout. As APIs não migram o banco no startup.
4. Migration nunca é editada depois de ir para a `main`; correções viram nova migration.

## 10.5 MongoDB — livro de doações

Banco `arrecadacao`, coleção `lancamentos_doacao`:

```json
{
  "_id": "0192f3c4-2b3c-7d4e-9f50-6a7b8c9d0e1f",
  "campanhaId": "0192f3b0-1a2b-7c3d-8e4f-5a6b7c8d9e0f",
  "doadorId": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
  "valor": { "$numberDecimal": "150.00" },
  "totalAposLancamento": { "$numberDecimal": "150.00" },
  "recebidaEm": { "$date": "2026-11-10T17:10:05Z" },
  "processadaEm": { "$date": "2026-11-10T17:10:06Z" },
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "versaoEsquema": 1
}
```

| Item | Definição |
|---|---|
| Chave | `_id = doacaoId` (idempotência natural) |
| Índices | `{ campanhaId: 1, processadaEm: -1 }` |
| Escrita | `ReplaceOne` com `IsUpsert = true`; nunca há atualização de lançamento existente com valor diferente |
| Valores | `Decimal128` (nunca `double` para dinheiro) |
| Driver | `MongoDB.Driver` oficial, cliente singleton |
| Uso | Auditoria e conferência (`SUM` por campanha deve bater com `total_arrecadado`) |

## 10.6 Redis — cache

| Chave | Conteúdo | Expiração | Quem remove |
|---|---|---|---|
| `conexao:transparencia:v1:campanhas-ativas` | JSON da resposta do painel | 30 s (L2 Redis), 5 s (L1 em memória) | Worker após cada doação; Campanhas.Api ao criar/alterar campanha |

- Implementação: `HybridCache` (.NET) com Redis como cache distribuído: protege contra *cache stampede* e mantém cópia local curta.
- Cache-aside: se o Redis falhar, a API consulta o banco e registra `Warning` (o cache é descartável).
- `maxmemory-policy allkeys-lru`, sem persistência em disco (dados reconstruíveis).

## 10.7 Elasticsearch — busca

Índice `campanhas-v1` com alias `campanhas` (reindexação sem indisponibilidade).

```json
{
  "settings": {
    "number_of_shards": 1,
    "number_of_replicas": 0,
    "analysis": {
      "analyzer": {
        "pt_br": {
          "tokenizer": "standard",
          "filter": ["lowercase", "asciifolding", "stop_pt", "stemmer_pt"]
        }
      },
      "filter": {
        "stop_pt": { "type": "stop", "stopwords": "_brazilian_" },
        "stemmer_pt": { "type": "stemmer", "language": "brazilian" }
      }
    }
  },
  "mappings": {
    "properties": {
      "titulo": { "type": "text", "analyzer": "pt_br", "fields": { "exato": { "type": "keyword", "normalizer": "lowercase" } } },
      "descricao": { "type": "text", "analyzer": "pt_br" },
      "status": { "type": "keyword" },
      "metaFinanceira": { "type": "scaled_float", "scaling_factor": 100 },
      "dataFim": { "type": "date" },
      "versao": { "type": "long" }
    }
  }
}
```

Consulta:
```json
{
  "query": {
    "bool": {
      "must": {
        "multi_match": {
          "query": "natl solidaro",
          "fields": ["titulo^3", "descricao"],
          "fuzziness": "AUTO",
          "prefix_length": 1
        }
      },
      "should": [ { "match_phrase": { "titulo": { "query": "natl solidaro", "boost": 5 } } } ],
      "filter": [ { "term": { "status": "Ativa" } } ]
    }
  },
  "highlight": { "fields": { "descricao": {} } },
  "sort": ["_score", { "dataFim": "asc" }]
}
```

| Item | Definição |
|---|---|
| Cliente | `Elastic.Clients.Elasticsearch` (NEST está descontinuado), singleton |
| Sincronização | `CampanhaAlteradaEvent` → upsert com `versao` como versão externa (ignora eventos fora de ordem) |
| Segurança | Autenticação por API key; não exposto pelo Kong |
| Local | Single-node, heap 512 MB, segurança básica ligada |

## 10.8 Backup e retenção (local e cloud opcional)

| Dado | Retenção | Backup |
|---|---|---|
| PostgreSQL | Enquanto a campanha existir | `pg_dump` manual antes de cada demonstração (`scripts/backup.ps1`) |
| MongoDB | Indefinida (prestação de contas) | `mongodump` no mesmo script |
| Redis | 30 s | Nenhum (descartável) |
| Elasticsearch | Reconstruível | Reindexação a partir do PostgreSQL |
| Logs | 7 dias (local) | Nenhum |

## 10.9 Dados de demonstração

Script `scripts/seed-demo.ps1` (via API, não SQL direto) cria: 1 gestor (seed), 3 campanhas ativas, 1 concluída e 1 cancelada, 2 doadores fictícios com CPFs válidos de teste. Nunca usar dados pessoais reais.
