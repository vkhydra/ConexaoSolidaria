# Feature 005 — Doação assíncrona

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/005-doacao` |
| Status | Especificada |
| Origem | Enunciado, requisito 5 (RF05) e requisito técnico de mensageria |
| Histórias | H06, H07 |
| Prioridade | P1 (núcleo do Hackathon) |
| Serviços | `Campanhas.Api` (recebe) e `Arrecadacao.Worker` (processa) |
| Acesso | `Doador` |
| Depende de | [001](../001-autenticacao/spec.md), [002](../002-gestao-campanhas/spec.md), [003](../003-cadastro-doador/spec.md), [004](../004-painel-transparencia/spec.md) |

## Objetivo

Permitir que o doador doe para uma campanha apta. A API aceita a doação e publica um evento; o Worker soma o valor ao total arrecadado, de forma assíncrona, sem perder nem duplicar doações.

## Histórias de usuário

> **H06** — Como doador, quero doar para uma campanha ativa, para ajudar a ONG.
>
> **H07** — Como gestor, quero que o total seja atualizado automaticamente após cada doação, para acompanhar a meta.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Doação
  Contexto:
    Dado um doador autenticado

  Cenário: Doar para campanha ativa
    Dado a campanha "Natal Solidário" ativa
    Quando ele doa 150.00
    Então recebe status 202 com um doacaoId
    E o evento "DoacaoRecebidaEvent" é publicado
    E o total da campanha não é alterado pela API

  Esquema do Cenário: Doar para campanha não apta
    Dado a campanha "X" com status "<status>"
    Quando ele doa 50.00
    Então recebe status 422
    E o código do erro é "doacao.campanha_nao_ativa"
    E nenhum evento é publicado

    Exemplos:
      | status    |
      | Concluida |
      | Cancelada |

  Cenário: Campanha inexistente
    Quando ele doa 50.00 para uma campanha que não existe
    Então recebe status 404

  Cenário: Valor inválido
    Quando ele doa 0.00 para uma campanha ativa
    Então recebe status 400

  Cenário: Mesma chave de idempotência
    Dado uma doação feita com a chave de idempotência "abc-123"
    Quando ele repete a doação com a chave "abc-123"
    Então recebe status 202 com o mesmo doacaoId
    E apenas um evento é publicado

  Cenário: Worker soma ao total
    Dado a campanha "Natal Solidário" com total 0.00
    Quando ele doa 150.00
    Então em até 5 segundos o total da campanha é 150.00
    E o livro de doações tem um lançamento de 150.00

  Cenário: Evento duplicado
    Dado um "DoacaoRecebidaEvent" de 100.00 já processado
    Quando o mesmo evento é entregue de novo ao Worker
    Então o total da campanha aumenta apenas 100.00

  Cenário: Gestor não doa
    Dado um gestor autenticado
    Quando ele tenta doar 50.00
    Então recebe status 403
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | A doação DEVE receber `idCampanha` e `valorDoacao` (> 0, até 2 casas, máximo 1.000.000,00). |
| FR-002 | A campanha DEVE existir (404) e estar apta: `Ativa` e `dataInicio ≤ hoje ≤ dataFim` (senão 422 `doacao.campanha_nao_ativa`). |
| FR-003 | A API NÃO DEVE alterar o total arrecadado. |
| FR-004 | A API DEVE gerar `doacaoId` (GUID v7), gravar `DoacaoRecebidaEvent` no Outbox na mesma transação e responder 202. |
| FR-005 | O cabeçalho opcional `Idempotency-Key` DEVE fazer a repetição em 24 h devolver o mesmo `doacaoId`, sem novo evento. |
| FR-006 | O Worker DEVE ignorar `doacaoId` já processado (idempotência). |
| FR-007 | O Worker DEVE somar o valor com `UPDATE` atômico, mesmo que a campanha tenha sido encerrada depois do aceite. |
| FR-008 | O Worker DEVE gravar o lançamento no livro de doações (MongoDB), também em reentregas. |
| FR-009 | O Worker DEVE invalidar o cache do painel e publicar `DoacaoProcessadaEvent` (Outbox). |
| FR-010 | Falhas no Worker DEVEM ter 3 retentativas imediatas + 3 com intervalo (5 s, 30 s, 2 min) e depois ir para a fila de erro. |
| FR-011 | Eventos NÃO DEVEM conter dados pessoais; o doador é identificado só pelo ID. |

## Casos de borda

- RabbitMQ fora do ar no momento da doação → a API responde 202 normalmente; o Outbox entrega quando o broker voltar.
- MongoDB fora do ar → retentativas; na reentrega o total não é somado de novo, mas o lançamento é gravado.
- Campanha cancelada entre o aceite e o processamento → o valor é somado (a doação já foi aceita).
- `Idempotency-Key` repetida com valor ou campanha diferente → 422 `doacao.chave_idempotencia_conflitante`.

## Entidades-chave

- **Doação recebida** (evento), **doação processada** (registro de idempotência), **lançamento do livro de doações** ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | `POST /doacoes` p95 < 500 ms com 20 VUs (RNF02). |
| SC-002 | Total visível no painel em até 5 s em 95% das doações (RNF03). |
| SC-003 | Nenhuma doação somada duas vezes em teste de reentrega (RNF12). |
| SC-004 | Nenhuma doação aceita é perdida com RabbitMQ parado durante o aceite (RNF11). |
| SC-005 | Soma do livro de doações por campanha igual ao `total_arrecadado`. |

## Fora do escopo

Pagamento real (cartão, Pix), estorno, doação recorrente e doação anônima sem login.

## Esclarecimentos

### Sessão 2026-10-30
- **P:** A doação é considerada paga no envio? → **R:** Sim. O enunciado não pede gateway de pagamento ([visão geral](../../product/overview.md)).
