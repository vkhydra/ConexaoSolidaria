# Feature 007 — Recibo de doação

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/007-recibo-doacao` |
| Status | Especificada |
| Origem | Fase 3 (serverless) — RF07 |
| História | H09 |
| Prioridade | P2 |
| Componente | `Notificacoes.Function` (Azure Functions isolated worker + KEDA) |
| Depende de | [005 — Doação](../005-doacao/spec.md) (`DoacaoProcessadaEvent`) |

## Objetivo

Emitir um recibo para cada doação processada, usando uma função serverless acionada por evento que escala a zero quando não há doações.

## História de usuário

> **H09** — Como doador, quero receber um recibo da doação, para ter um comprovante.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Recibo de doação

  Cenário: Recibo após processamento
    Quando uma doação de 150.00 é processada
    Então a função registra "ReciboEmitido" com o doacaoId e o valor 150.00
    E o registro não contém nome, e-mail nem CPF

  Cenário: Evento repetido não gera recibo duplicado
    Dado um recibo já emitido para a doação "D1"
    Quando o evento da doação "D1" é entregue de novo
    Então nenhum novo "ReciboEmitido" é registrado para "D1"

  Cenário: Escala a zero
    Dado a fila de recibos vazia por mais de 60 segundos
    Então a função fica com 0 réplicas

  Cenário: Escala sob demanda
    Dado a função com 0 réplicas
    Quando uma doação é processada
    Então uma réplica da função sobe e emite o recibo
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | A função DEVE ser disparada por `DoacaoProcessadaEvent` na fila `notificacoes-recibo`. |
| FR-002 | A função NÃO DEVE consultar dados pessoais; o recibo usa só os dados do evento (IDs, valor, total, datas). |
| FR-003 | O "envio" DEVE ser simulado por log estruturado `Information` com `ReciboEmitido`, `doacaoId`, `campanhaId` e `valor`. |
| FR-004 | A emissão DEVE ser idempotente por `doacaoId`. |
| FR-005 | A função DEVE escalar de 0 a 3 réplicas pelo tamanho da fila (KEDA), voltando a 0 após 60 s sem mensagens. |
| FR-006 | Mensagem inválida DEVE ir para a fila `notificacoes-recibo_error` após as retentativas. |

## Casos de borda

- Envelope do MassTransit com campos extras → ignorados; só `message` é lido.
- Evento de versão desconhecida (`V2`) → rejeitado para a fila de erro com log `Warning`.
- Reinício da função durante a emissão → reentrega; a deduplicação evita recibo duplicado.

## Entidades-chave

- **Recibo** (registro de log de negócio) e **chave de deduplicação** ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | Cada doação processada gera exatamente um `ReciboEmitido`. |
| SC-002 | A função fica em 0 réplicas com a fila vazia (RNF07). |
| SC-003 | Nenhum dado pessoal no log da função (teste automatizado). |

## Fora do escopo

Envio real de e-mail, PDF do recibo e recibo com dados fiscais do doador.

## Esclarecimentos

### Sessão 2026-10-30
- **P:** Onde guardar a deduplicação, já que a função não tem banco? → **R:** no Redis, chave `conexao:recibos:v1:<doacaoId>` com expiração de 7 dias ([pesquisa R3](research.md)).
