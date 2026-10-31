# Feature 004 — Painel de transparência

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/004-painel-transparencia` |
| Status | Especificada |
| Origem | Requisito do domínio da API (RF04); testado conforme a [feature 009](../009-ci-testes-cobertura/spec.md) |
| História | H05 |
| Prioridade | P1 |
| Serviço | `Campanhas.Api` |
| Acesso | Público |
| Depende de | [002 — Gestão de campanhas](../002-gestao-campanhas/spec.md) |

## Objetivo

Mostrar a qualquer pessoa as campanhas em andamento, a meta e quanto já foi arrecadado, para dar transparência às doações.

## História de usuário

> **H05** — Como visitante, quero ver campanhas ativas e o total arrecadado, para confiar na ONG.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Painel de transparência

  Cenário: Apenas campanhas ativas
    Dado as campanhas "A" (Ativa), "B" (Concluida) e "C" (Cancelada)
    Quando um visitante consulta o painel
    Então vê apenas a campanha "A"

  Cenário: Campanha com prazo encerrado não aparece
    Dado hoje é "2026-12-25"
    E a campanha "Natal Solidário" ativa com data fim "2026-12-24"
    Quando um visitante consulta o painel
    Então a campanha "Natal Solidário" não aparece

  Cenário: Campanha que ainda não começou não aparece
    Dado hoje é "2026-11-10"
    E a campanha "Inverno" ativa com data início "2026-12-01"
    Quando um visitante consulta o painel
    Então a campanha "Inverno" não aparece

  Cenário: Percentual atingido
    Dado a campanha "A" com meta 10000.00 e total arrecadado 150.00
    Quando um visitante consulta o painel
    Então o percentual atingido de "A" é 1.50

  Cenário: Total após doação processada
    Dado a campanha "A" com total arrecadado 0.00
    Quando uma doação de 150.00 para "A" é processada pelo Worker
    Então em até 5 segundos o painel mostra total 150.00 para "A"

  Cenário: Painel funciona sem o cache
    Dado o Redis fora do ar
    Quando um visitante consulta o painel
    Então recebe status 200 com os dados do banco
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | O painel DEVE ser público. |
| FR-002 | O painel DEVE listar só campanhas `Ativa` com `dataInicio ≤ hoje ≤ dataFim`, ordenadas por data fim, no máximo 100. |
| FR-003 | Cada item DEVE trazer `id`, `titulo`, `metaFinanceira`, `totalArrecadado`, `percentualAtingido` (2 casas) e `dataFim`. |
| FR-004 | O `totalArrecadado` DEVE vir da soma das doações processadas pelo Worker, nunca da API de doação. |
| FR-005 | A resposta DEVE ficar em cache (Redis via `HybridCache`) por 30 s e ser invalidada pelo Worker a cada doação processada e pela API a cada alteração de campanha. |
| FR-006 | A resposta DEVE ter `Cache-Control: public, max-age=10`. |
| FR-007 | Falha do Redis NÃO DEVE derrubar o painel: a consulta vai ao banco e registra `Warning`. |
| FR-008 | A interface DEVE avisar que os valores são atualizados em até alguns segundos (consistência eventual). |

## Casos de borda

- Nenhuma campanha ativa → 200 com lista vazia.
- Total maior que a meta → percentual acima de 100 (sem limite).
- Campanha cancelada depois de entrar no cache → some em até 30 s ou na próxima invalidação.

## Entidades-chave

- **Item do painel** (modelo de leitura, sem agregado) ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | p95 < 300 ms com 50 usuários virtuais (RNF01, k6 `painel.js`). |
| SC-002 | Total atualizado em até 5 s em 95% das doações (RNF03). |
| SC-003 | Disponibilidade do painel ≥ 99,5% (SLO). |

## Fora do escopo

Lista de doações por campanha, nomes de doadores e histórico de campanhas encerradas.
