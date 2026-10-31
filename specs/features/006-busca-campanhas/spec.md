# Feature 006 — Busca de campanhas

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/006-busca-campanhas` |
| Status | Especificada |
| Origem | Fase 4 (Elasticsearch) — RF06 |
| História | H08 |
| Prioridade | P2 |
| Serviço | `Campanhas.Api` |
| Acesso | Público |
| Depende de | [002 — Gestão de campanhas](../002-gestao-campanhas/spec.md) (`CampanhaAlteradaEvent`) |

## Objetivo

Permitir que qualquer pessoa encontre campanhas ativas digitando parte do nome ou da descrição, mesmo com erros de digitação, com os resultados mais relevantes primeiro.

## História de usuário

> **H08** — Como visitante, quero buscar campanhas mesmo com erro de digitação, para achá-las rápido.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Busca de campanhas

  Cenário: Erro de digitação
    Dado a campanha ativa "Natal Solidário"
    Quando um visitante busca "natl solidaro"
    Então "Natal Solidário" aparece no resultado

  Cenário: Título vale mais que descrição
    Dado a campanha ativa "Agasalho" com descrição "Roupas de inverno"
    E a campanha ativa "Inverno sem fome" com descrição "Cestas básicas"
    Quando um visitante busca "inverno"
    Então "Inverno sem fome" aparece antes de "Agasalho"

  Cenário: Campanha cancelada não aparece
    Dado a campanha cancelada "Inverno 2026"
    Quando um visitante busca "inverno"
    Então o resultado é vazio

  Cenário: Campanha nova entra na busca
    Dado um gestor que cria a campanha "Páscoa Feliz"
    Quando um visitante busca "pascoa" 5 segundos depois
    Então "Páscoa Feliz" aparece no resultado

  Cenário: Termo curto demais
    Quando um visitante busca "a"
    Então recebe status 400

  Cenário: Busca indisponível
    Dado o Elasticsearch fora do ar
    Quando um visitante busca "natal"
    Então recebe status 503
    E o painel de transparência continua respondendo
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | A busca DEVE ser pública: `q` com 2 a 100 caracteres, paginação `page`/`pageSize` (padrão 10, máximo 50). |
| FR-002 | A busca DEVE olhar título (peso 3) e descrição (peso 1), com tolerância a erros (`fuzziness: AUTO`, `prefix_length: 1`). |
| FR-003 | Correspondência de frase no título DEVE receber peso maior. |
| FR-004 | Acentos e maiúsculas NÃO DEVEM afetar o resultado ("pascoa" encontra "Páscoa"). |
| FR-005 | Só campanhas `Ativa` DEVEM aparecer (filtro, sem afetar a relevância). |
| FR-006 | A ordenação DEVE ser por relevância e, no empate, por data fim. |
| FR-007 | O resultado DEVE trazer um trecho destacado da descrição. |
| FR-008 | O índice DEVE ser atualizado por evento em até 5 s após criar ou alterar a campanha. |
| FR-009 | Elasticsearch fora do ar DEVE resultar em 503 com `ProblemDetails`, sem derrubar o resto da API. |
| FR-010 | O gestor DEVE poder reindexar todas as campanhas (`POST /admin/reindexar`). |

## Casos de borda

- Eventos fora de ordem → a versão mais nova vence (versão externa no índice).
- Busca só com palavras vazias ("de", "a") → resultado vazio, 200.
- Caracteres especiais na busca → tratados como texto, nunca como sintaxe de consulta.

## Entidades-chave

- **Documento de campanha no índice** ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | p95 < 400 ms com 30 VUs (RNF04, k6 `busca.js`). |
| SC-002 | 100% dos cenários de erro de digitação da spec encontram a campanha. |
| SC-003 | Campanha criada aparece na busca em até 5 s. |

## Fora do escopo

Sugestões de busca (autocomplete), filtros por faixa de meta e busca por doadores.
