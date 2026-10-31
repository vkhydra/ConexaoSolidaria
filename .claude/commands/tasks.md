---
description: Quebra o plano da feature em tarefas ordenadas (tasks.md), com TDD, arquivos e mensagem de commit em cada tarefa
argument-hint: "[NNN da feature; padrão: a da branch atual]"
---

# /tasks

Gere `tasks.md` da feature **$ARGUMENTS** (se vazio, use a branch atual `feat/NNN-*`).

## Pré-condições

`spec.md`, `plan.md` e `data-model.md` existem. Se faltar algum, oriente o comando que falta e pare.

## Passos

1. Leia spec, plano, pesquisa, modelo de dados, contratos, `specs/constitution.md` e `docs/git-e-commits.md` (gitmojis e escopos).
2. Derive as tarefas:
   - cada FR, cenário Gherkin e endpoint do contrato vira pelo menos uma tarefa;
   - cada entidade/VO do modelo de dados vira tarefa de domínio com TDD;
   - cada evento vira tarefa de contrato com teste de snapshot;
   - cada página da interface vira tarefa com teste bUnit.
3. Organize por fases, nesta ordem: Domínio (TDD) → Contratos → Aplicação → Infraestrutura → API/consumidores → Testes de integração e aceitação → Interface → Fechamento.
4. Cada tarefa é **um commit** que compila e passa nos testes (constituição, princípio VI). Teste e implementação mínima ficam na mesma tarefa.
5. Marque `[P]` só quando a tarefa não depende da anterior e mexe em arquivos diferentes.
6. Última fase sempre com: `/code-review` (e `/security-review` se houver autenticação, dados pessoais ou segredos), cobertura ≥ 90% (skill `verificar-cobertura`) e `git rebase --exec`.
7. Termine com a seção `## Dependências` (ex.: `T001 → T003`).

## Formato

```markdown
# Tarefas — Feature NNN (<Título>)

[← Plano](plan.md)

## Fase 1 — Domínio (TDD)

- [ ] T001 [Hnn] <o que fazer, com os testes> — `<caminho/arquivo.cs>`
  Commit: `<gitmoji> <tipo>(<escopo>): <descrição no imperativo>`
- [ ] T002 [P] [Hnn] ...
```

## Validação

- [ ] Todo FR da spec aparece em alguma tarefa.
- [ ] Todo cenário Gherkin tem tarefa de automação em `tests/e2e/ConexaoSolidaria.Bdd/Features/NNN-nome.feature`.
- [ ] Toda mensagem de commit passa na regex de `docs/git-e-commits.md`.
- [ ] Nenhuma tarefa deixa o build quebrado sozinha.

Commit com a skill `commit-gitmoji`: `📝 docs(specs): adiciona tarefas da feature NNN <nome>`.

## Saída

Total de tarefas por fase, quantas `[P]`, caminho crítico e próximo passo (`/analyze`).
