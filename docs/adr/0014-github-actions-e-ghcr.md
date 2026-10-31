# ADR 0014 — GitHub Actions e GHCR

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

o enunciado exige CI a cada push na branch principal com build e imagem Docker. Repositório público no GitHub.

## Decisão

GitHub Actions; imagens no GitHub Container Registry (GHCR) com tag `sha-<commit>` e `vX.Y.Z`.

## Alternativas consideradas

Azure DevOps (Pipelines e Boards): exigiria projeto separado; Docker Hub: limites de pull.

## Consequências

Azure DevOps e AWS ficam como alternativas registradas, não implementadas.

## Referências

- [CI/CD](../../specs/product/ci-cd.md)
