# ADR 0017 — Spec-Driven Development com Claude Code

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O projeto tem 8 requisitos funcionais, 4 componentes de backend, frontend e uma plataforma completa (Kubernetes, gateway, observabilidade, CI/CD), feitos por um grupo pequeno em poucas semanas. O grupo usa o Claude Code como assistente. Sem um processo explícito, o código gerado tende a se afastar dos requisitos e das regras do projeto (TDD, cobertura de 90%, `<summary>`, commits).

## Decisão

Adotar **Spec-Driven Development** (no formato do GitHub Spec Kit) com comandos do Claude Code versionados no repositório:

1. `specs/constitution.md`: princípios inegociáveis, verificados em todo plano e revisão.
2. `specs/product/`: visão do produto e decisões transversais (arquitetura, dados, segurança, observabilidade, infraestrutura, CI/CD).
3. `specs/features/NNN-nome/`: uma pasta por funcionalidade com `spec.md` (o quê e por quê), `research.md`, `plan.md` (como), `data-model.md`, `contracts/` e `tasks.md` (passos com commit).
4. Fluxo por comandos: `/constitution` → `/specify` → `/clarify` → `/plan` → `/tasks` → `/analyze` → `/implement`.
5. Subagentes com papéis fixos (`architect`, `implementer`, `test-writer`, `reviewer`) e skills para tarefas repetitivas (commit, ADR, cobertura, documentação XML).

## Alternativas consideradas

- **Especificação única em `docs/`** (formato anterior do projeto): boa para leitura, mas não separa o que é de cada funcionalidade nem guia a implementação passo a passo.
- **GitHub Spec Kit via CLI (`specify init`):** traz templates em inglês e scripts próprios; os comandos versionados em português dão o mesmo fluxo sem outra ferramenta.
- **Sem processo:** mais rápido no começo, com risco alto de retrabalho.

## Consequências

- Toda funcionalidade nova começa com `/specify`, nunca direto no código.
- A rastreabilidade fica explícita: RF → `spec.md` → `tasks.md` → commit → teste.
- Mudança de princípio exige alterar a constituição (com versão) antes do código.
- Os documentos de `specs/` passam a ser revisados no PR como o código.

## Referências

- [Constituição](../../specs/constitution.md)
- [Claude Code no projeto](../claude-code.md)
- [Plano de execução](../plano-de-execucao.md)
