# Feature 002 — Gestão de campanhas

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/002-gestao-campanhas` |
| Status | Especificada |
| Origem | Enunciado, requisito 2 (RF02) |
| Histórias | H03, H04 |
| Prioridade | P1 |
| Serviço | `Campanhas.Api` |
| Acesso | `GestorONG` |
| Depende de | [001 — Autenticação](../001-autenticacao/spec.md) |

## Objetivo

Permitir que o gestor da ONG crie e mantenha campanhas de arrecadação com meta financeira e período, garantindo regras que impedem campanhas inválidas.

## Histórias de usuário

> **H03** — Como gestor, quero criar campanhas com meta e período, para arrecadar.
>
> **H04** — Como gestor, quero editar, concluir e cancelar campanhas, para mantê-las corretas.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Gestão de campanhas
  Contexto:
    Dado um gestor autenticado
    E hoje é "2026-11-10"

  Cenário: Criar campanha válida
    Quando ele cria a campanha "Natal Solidário" de "2026-11-10" a "2026-12-24" com meta 10000.00
    Então recebe status 201
    E a campanha tem status "Ativa" e total arrecadado 0.00

  Esquema do Cenário: Criar campanha inválida
    Quando ele cria uma campanha com data fim "<dataFim>" e meta <meta>
    Então recebe status <status>
    E o código do erro é "<codigo>"

    Exemplos:
      | dataFim    | meta     | status | codigo                     |
      | 2026-11-09 | 1000.00  | 422    | campanha.data_fim_passada  |
      | 2026-12-01 | 0.00     | 422    | campanha.meta_invalida     |
      | 2026-12-01 | -10.00   | 422    | campanha.meta_invalida     |

  Cenário: Data fim anterior à data início
    Quando ele cria uma campanha de "2026-12-10" a "2026-12-01" com meta 1000.00
    Então recebe status 422
    E o código do erro é "campanha.periodo_invalido"

  Cenário: Editar campanha ativa
    Dado a campanha "Natal Solidário" ativa na versão "1"
    Quando ele altera a meta para 12000.00 informando a versão "1"
    Então recebe status 204

  Cenário: Edição concorrente
    Dado a campanha "Natal Solidário" já alterada para a versão "2"
    Quando ele altera a campanha informando a versão "1"
    Então recebe status 409
    E o código do erro é "campanha.conflito_de_versao"

  Cenário: Cancelar campanha concluída
    Dado uma campanha com status "Concluida"
    Quando ele tenta cancelar a campanha
    Então recebe status 422
    E o código do erro é "campanha.transicao_invalida"

  Cenário: Doador não gerencia campanhas
    Dado um doador autenticado
    Quando ele tenta criar uma campanha
    Então recebe status 403
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | O gestor DEVE criar campanha com título (3–120), descrição (até 2000), data início, data fim e meta financeira. |
| FR-002 | A data fim NÃO DEVE ser anterior a hoje nem à data início. |
| FR-003 | A meta DEVE ser maior que zero e até 99.999.999,99, com até 2 casas decimais. |
| FR-004 | Toda campanha DEVE nascer com status `Ativa` e total arrecadado 0. |
| FR-005 | O gestor DEVE listar campanhas com paginação (`page` ≥ 1, `pageSize` 1–100, padrão 20), filtro opcional por status e ordenação por data fim. |
| FR-006 | O gestor DEVE detalhar uma campanha por ID (404 se não existir). |
| FR-007 | Só campanhas `Ativa` PODEM ser editadas, com as mesmas regras da criação. |
| FR-008 | A edição DEVE exigir a versão atual (`If-Match`); versão divergente → 409; sem `If-Match` → 412. |
| FR-009 | As únicas transições DEVEM ser `Ativa → Concluida` e `Ativa → Cancelada`; qualquer outra → 422. |
| FR-010 | O total arrecadado NUNCA PODE ser alterado por esta feature. |
| FR-011 | Toda criação e alteração DEVE publicar `CampanhaAlteradaEvent` (usado pela [busca](../006-busca-campanhas/spec.md)). |
| FR-012 | "Hoje" DEVE ser a data atual em `America/Sao_Paulo`. |

## Casos de borda

- Título com espaços nas pontas → 400 (não é corrigido em silêncio).
- Meta com 3 casas decimais → 400.
- Data fim igual a hoje → aceita.
- Editar campanha concluída ou cancelada → 422 `campanha.nao_editavel`.
- Alterar status para o mesmo status atual → 422 `campanha.transicao_invalida`.

## Entidades-chave

- **Campanha** (raiz do agregado), **Periodo** e **Dinheiro** (value objects), **StatusCampanha** ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | Todas as regras de FR-002, FR-003, FR-007 e FR-009 cobertas por testes de unidade do agregado. |
| SC-002 | Campanha criada aparece na busca em até 5 s (via evento). |
| SC-003 | Nenhuma rota de gestão responde a doador ou visitante (403/401). |

## Fora do escopo

Exclusão física de campanhas, upload de imagens, categorias e campanhas recorrentes.

## Esclarecimentos

### Sessão 2026-10-30
- **P:** Uma campanha pode ser criada com data início no futuro? → **R:** Sim. Ela fica `Ativa`, mas só aparece no painel e recebe doações quando a data início chegar ([004](../004-painel-transparencia/spec.md), [005](../005-doacao/spec.md)).
