# Modelo de dados — Feature 004 (Painel de transparência)

[← Spec](spec.md)

## Modelo de leitura `PainelItemResponse`

| Campo | Tipo | Origem |
|---|---|---|
| `Id` | `Guid` | `campanhas.id` |
| `Titulo` | `string` | `campanhas.titulo` |
| `MetaFinanceira` | `decimal` | `campanhas.meta_financeira` |
| `TotalArrecadado` | `decimal` | `campanhas.total_arrecadado` (escrito só pelo Worker) |
| `PercentualAtingido` | `decimal` | Calculado: `round(total / meta × 100, 2)` |
| `DataFim` | `DateOnly` | `campanhas.data_fim` |

## Consulta (Dapper)

```sql
SELECT id, titulo,
       meta_financeira  AS MetaFinanceira,
       total_arrecadado AS TotalArrecadado,
       data_fim         AS DataFim
  FROM campanhas
 WHERE status = 'Ativa'
   AND data_inicio <= @Hoje
   AND data_fim    >= @Hoje
 ORDER BY data_fim
 LIMIT 100;
```

Índice usado: `ix_campanhas_status_data_fim (status, data_fim)` ([002](../002-gestao-campanhas/data-model.md)).

## Cache (Redis)

| Chave | Conteúdo | Expiração | Quem remove |
|---|---|---|---|
| `conexao:transparencia:v1:campanhas-ativas` | JSON da lista do painel | L2 30 s; L1 5 s | Worker após cada doação; `Campanhas.Api` após criar ou alterar campanha |

`maxmemory-policy allkeys-lru`, sem persistência em disco (dados reconstruíveis). Visão geral: [dados](../../product/dados.md).
