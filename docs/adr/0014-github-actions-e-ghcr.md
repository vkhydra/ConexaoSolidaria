# ADR 0014 — GitHub Actions e GHCR

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

O trabalho substitutivo exige uma pipeline de CI (GitHub Actions ou Azure DevOps) que execute os testes e retorne a cobertura ([ADR 0018](0018-escopo-trabalho-substitutivo.md)). A Fase 4 também ensina build e publicação de imagens. O repositório está no GitHub.

## Decisão

GitHub Actions; imagens no GitHub Container Registry (GHCR) com tag `sha-<commit>` e `vX.Y.Z`.

## Alternativas consideradas

Azure DevOps (Pipelines e Boards): exigiria projeto separado; Docker Hub: limites de pull.

## Consequências

Azure DevOps e AWS ficam como alternativas registradas, não implementadas.

## Referências

- [CI/CD](../../specs/product/ci-cd.md)
