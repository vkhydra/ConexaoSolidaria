# Pesquisa — Feature 004 (Painel de transparência)

[← Spec](spec.md)

## R1 — Consulta de leitura

- **Decisão:** Dapper com SQL direto sobre `campanhas`, usando o índice `(status, data_fim)`.
- **Justificativa:** consulta mais lida do sistema; sem rastreamento do EF Core; aplica conteúdo de Dapper da Fase 1.
- **Alternativas:** EF Core com `AsNoTracking` e projeção (bom, mas o Dapper deixa o SQL explícito para a demonstração).

## R2 — Cache

- **Decisão:** `HybridCache` com L1 em memória (5 s) e L2 no Redis (30 s); chave `conexao:transparencia:v1:campanhas-ativas`.
- **Justificativa:** protege contra *cache stampede* (uma só consulta ao banco por chave) e reduz idas ao Redis.
- **Alternativas:** `IDistributedCache` puro (sem proteção de stampede); `IMemoryCache` (cada réplica com um valor diferente).
- **Fonte:** MCP Microsoft Learn, "HybridCache library in ASP.NET Core".

## R3 — Invalidação pelo Worker

- **Decisão:** o Worker remove a chave no Redis após cada doação processada; a API faz o mesmo ao alterar campanhas.
- **Justificativa:** o L1 de 5 s em cada réplica limita a defasagem; o requisito de 5 s (RNF03) é atendido.
- **Risco:** com `HybridCache`, a remoção no L2 não limpa o L1 das outras réplicas; aceito porque o L1 dura só 5 s.

## R4 — Percentual atingido

- **Decisão:** calculado na consulta (`total / meta × 100`, arredondado para 2 casas com arredondamento bancário).
- **Justificativa:** valor derivado; não é guardado no banco.

## R5 — Data de referência

- **Decisão:** `@Hoje` vem do mesmo `IRelogio` da [feature 002](../002-gestao-campanhas/research.md) (São Paulo).
