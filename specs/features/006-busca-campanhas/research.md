# Pesquisa — Feature 006 (Busca de campanhas)

[← Spec](spec.md)

## R1 — Motor de busca

- **Decisão:** Elasticsearch 9, single-node no local, com cliente oficial `Elastic.Clients.Elasticsearch`.
- **Justificativa:** índice invertido, análise em português, fuzzy e relevância (`_score`); conteúdo da Fase 4.
- **Alternativas:** `ILIKE`/`tsvector` do PostgreSQL (sem tolerância a erro equivalente); NEST (descontinuado).
- **Registro:** [ADR 0007](../../../docs/adr/0007-persistencia-poliglota.md).

## R2 — Análise de texto em português

- **Decisão:** analyzer `pt_br` com `lowercase`, `asciifolding`, stopwords `_brazilian_` e stemmer `brazilian`.
- **Justificativa:** "Páscoa" e "pascoa" viram o mesmo termo; plurais e flexões se aproximam.

## R3 — Sincronização com o banco

- **Decisão:** consumidor `IndexarCampanhaConsumer` (na própria `Campanhas.Api`) recebe `CampanhaAlteradaEvent` e faz upsert usando `versao` como versão externa (`version_type: external`).
- **Justificativa:** eventos fora de ordem não sobrescrevem dados novos; a API continua dona dos dados.
- **Alternativas:** escrita dupla no endpoint (perde atualização se o Elasticsearch cair); Logstash/CDC (mais infraestrutura).

## R4 — Reindexação sem indisponibilidade

- **Decisão:** índice `campanhas-v1` atrás do alias `campanhas`; reindexação cria `campanhas-vN`, carrega do PostgreSQL e troca o alias.
- **Justificativa:** mudança de mapping sem tirar a busca do ar.

## R5 — Falha do Elasticsearch

- **Decisão:** health check `Degraded` (não derruba a prontidão); a busca responde 503 `busca.indisponivel`.
- **Justificativa:** só a busca depende do Elasticsearch (RNF13).
