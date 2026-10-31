# ADR 0007 — Persistência poliglota

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

Padrões de acesso diferentes (transação financeira, histórico imutável, leitura pública intensa, busca textual). A Fase 4 exige NoSQL, cache e motor de busca.

## Decisão

PostgreSQL (dados transacionais), MongoDB (livro de doações), Redis (cache do painel), Elasticsearch (busca). Detalhes em [Dados](../../specs/product/dados.md).

## Alternativas consideradas

Só PostgreSQL (com `tsvector` para busca e tabela de auditoria): mais simples, mas não cobre NoSQL nem Elasticsearch; SQL Server: imagem mais pesada, sem vantagem aqui.

## Consequências

Mais componentes para operar; justificativa formal em `docs/justificativa-bancos.md`.

## Referências

- [Dados](../../specs/product/dados.md)
