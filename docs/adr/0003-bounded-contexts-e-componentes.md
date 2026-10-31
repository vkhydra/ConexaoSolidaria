# ADR 0003 — Três bounded contexts e quatro componentes de backend

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A Fase 2 ensina microsserviços e o grupo quer aplicá-los na API. Excesso de serviços aumenta o custo de operação local e de testes.

## Decisão

Contextos Identidade, Campanhas e Notificações; componentes `Identity.Api`, `Campanhas.Api`, `Arrecadacao.Worker` e `Notificacoes.Function`.

## Alternativas consideradas

2 serviços (API única + Worker): seria mais simples, mas misturaria identidade com arrecadação; um serviço por agregado: complexidade sem ganho.

## Consequências

Fronteiras alinhadas ao negócio; comunicação entre contextos só por JWT e eventos.

## Referências

- [Domínio](../../specs/product/dominio.md)
- [Arquitetura](../../specs/product/arquitetura.md)
