# ADR 0002 — Monorepo

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

o Hackathon pede **um** repositório público com README passo a passo.

## Decisão

um repositório, uma solução `ConexaoSolidaria.slnx`, projetos isolados por serviço e um Dockerfile por serviço.

## Alternativas consideradas

um repositório por serviço (como na Fase 2): mais burocracia para avaliar e para o CI.

## Consequências

CI com matriz por serviço; o teste de arquitetura impede referências cruzadas entre contextos.

## Referências

- [Arquitetura](../../specs/product/arquitetura.md)
