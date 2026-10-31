# ADR 0002 — Monorepo

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A entrega é um vídeo que mostra a API, os testes e a pipeline com cobertura. Um único repositório simplifica a pipeline, a cobertura consolidada e a demonstração.

## Decisão

Um repositório, uma solução `ConexaoSolidaria.slnx`, projetos isolados por serviço e um Dockerfile por serviço.

## Alternativas consideradas

Um repositório por serviço (como na Fase 2): mais burocracia para avaliar e para o CI.

## Consequências

CI com matriz por serviço; o teste de arquitetura impede referências cruzadas entre contextos.

## Referências

- [Arquitetura](../../specs/product/arquitetura.md)
