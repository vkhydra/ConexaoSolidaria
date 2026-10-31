---
description: Implementa as tarefas pendentes de tasks.md em ordem, com TDD, build, testes, cobertura e um commit por tarefa
argument-hint: "[NNN da feature] [Tnnn inicial opcional]"
---

# /implement

Implemente a feature **$ARGUMENTS** (se vazio, use a branch atual `feat/NNN-*`), tarefa por tarefa.

## Pré-condições (pare se alguma falhar)

1. Branch atual é `feat/NNN-*` da feature.
2. Árvore de trabalho limpa (`git status`).
3. `/analyze` sem achados CRÍTICOS (rode-o se não houver relatório recente nesta sessão).
4. Build atual passando: `dotnet build -c Release -warnaserror`.

## Ciclo por tarefa (a partir da primeira `- [ ]` ou do `Tnnn` informado)

1. Leia a tarefa, o FR/cenário de origem, o plano e o contrato.
2. **Vermelho:** escreva primeiro o teste (subagente `test-writer` quando a tarefa tiver regra de negócio). Rode e confirme que falha pelo motivo certo.
3. **Verde:** implemente o mínimo para passar (subagente `implementer`), seguindo `docs/padroes-de-codigo.md`:
   - `/// <summary>` em todo tipo e membro (skill `documentar-xml`);
   - sem dados pessoais em logs e eventos;
   - `CancellationToken` em todo I/O.
4. Valide:
   ```bash
   dotnet format --verify-no-changes
   dotnet build -c Release -warnaserror
   dotnet test -c Release --no-build
   ```
5. Marque a tarefa como `- [X]` em `tasks.md`.
6. Commit com a skill `commit-gitmoji`, usando a mensagem da tarefa. O commit inclui o código, os testes e a marcação no `tasks.md`.
7. Refatoração necessária vira commit separado `♻️ refactor(...)`.

Se a validação falhar, corrija antes de seguir. Se não for possível, pare e relate o erro, a tarefa e o que foi tentado.

## Ao terminar uma fase

- Rode a skill `verificar-cobertura` (mínimo 90% de linhas e ramos, total e por projeto).
- Migrations novas: skill `nova-migration`. Manifestos Kubernetes: skill `validar-k8s`.

## Ao terminar todas as tarefas

1. Subagente `reviewer` sobre o diff da branch; trate os apontamentos.
2. `/code-review` e, se houver autenticação, dados pessoais ou segredos, `/security-review`.
3. `git rebase --exec "dotnet build -c Release -warnaserror && dotnet test -c Release --no-build" origin/main` (pede confirmação).
4. Atualize o `Status` da spec para `Implementada`.
5. **Não** faça push nem abra PR sem pedido explícito do usuário.

## Saída

Tarefas concluídas, commits criados, cobertura final, achados de revisão pendentes e próximo passo (PR).
