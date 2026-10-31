# ADR 0003 — Três bounded contexts e quatro componentes de backend

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O enunciado exige ≥ 2 microsserviços. Excesso de serviços aumenta o custo de operação local.

## Decisão

Contextos Identidade, Campanhas e Notificações; componentes `Identity.Api`, `Campanhas.Api`, `Arrecadacao.Worker` e `Notificacoes.Function`.

## Alternativas consideradas

2 serviços (API única + Worker): cumpriria o mínimo, mas misturaria identidade com arrecadação; um serviço por agregado: complexidade sem ganho.

## Consequências

Fronteiras alinhadas ao negócio; comunicação entre contextos só por JWT e eventos.

## Referências

- [Domínio](../../specs/product/dominio.md)
- [Arquitetura](../../specs/product/arquitetura.md)
