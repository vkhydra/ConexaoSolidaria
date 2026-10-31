---
description: Cria ou altera a constituição do projeto (specs/constitution.md) e propaga a mudança para specs, comandos, agentes e skills
argument-hint: "[mudança desejada, ex.: acrescentar princípio de acessibilidade]"
---

# /constitution

Altere a constituição do Conexão Solidária conforme o pedido: **$ARGUMENTS**

## Passos

1. Leia `specs/constitution.md` inteiro e o `CLAUDE.md`.
2. Se `$ARGUMENTS` estiver vazio ou ambíguo, pergunte ao usuário o que mudar (no máximo 3 perguntas objetivas) antes de editar.
3. Classifique a mudança e calcule a nova versão (versionamento semântico, seção "Governança"):
   - **MAJOR:** remove ou redefine um princípio de forma incompatível;
   - **MINOR:** acrescenta princípio, seção ou amplia uma regra;
   - **PATCH:** só esclarece o texto.
4. Edite a constituição:
   - use **DEVE**, **NÃO DEVE** e **DEVERIA**;
   - todo princípio tem uma linha **Verificação** dizendo como é checado;
   - atualize a versão e a "Última alteração" (data de hoje, `AAAA-MM-DD`).
5. Propague a mudança e liste o que foi alterado:
   - `CLAUDE.md` (resumo das regras);
   - `.claude/commands/*.md` (principalmente a tabela de verificação de `/plan`);
   - `.claude/agents/*.md` e `.claude/skills/*/SKILL.md`;
   - `docs/padroes-de-codigo.md`, `docs/git-e-commits.md`, `docs/testes.md` e `docs/processo-agil.md` (DoD), quando afetados;
   - `plan.md` das features em andamento: acrescente uma linha na "Verificação da constituição" se o princípio for novo.
6. Faça um commit por arquivo alterado com a skill `commit-gitmoji` (tipo `docs`, escopo `specs`).

## Saída

Responda com:
- versão anterior → versão nova e o motivo do tipo de incremento;
- tabela "Arquivo | O que mudou";
- pendências que precisam de decisão humana.

## Regras

- Não altere princípios que o pedido não citou.
- Não diminua exigências (cobertura, `<summary>`, commits) sem pedido explícito do usuário.
- Escreva em português do Brasil.
