---
name: commit-gitmoji
description: Cria commits semânticos com gitmoji no padrão do Conexão Solidária (um arquivo por commit, build e testes passando, sem coautoria). Use sempre que for fazer commit neste repositório.
---

# Commit semântico com gitmoji

Regras completas em `docs/git-e-commits.md`. Resumo obrigatório:

```text
<gitmoji> <tipo>(<escopo opcional>): <descrição no imperativo, minúscula, sem ponto, até 72 caracteres>
```

## Passos

1. Veja o que mudou:
   ```bash
   git status --short
   git diff
   ```
2. Agrupe as mudanças:
   - **um arquivo criado = um commit**;
   - quando um arquivo só compila junto com outro (`.csproj` + `Program.cs`; interface + implementação + registro no DI; entidade + configuração EF + migration), use o **menor conjunto que compila**;
   - teste e implementação mínima juntos; refatoração em commit separado;
   - nunca misture mudanças sem relação.
3. Escolha gitmoji, tipo e escopo na tabela de `docs/git-e-commits.md`. Os mais usados:

   | Gitmoji | Tipo | Quando |
   |---|---|---|
   | ✨ | `feat` | Nova funcionalidade |
   | 🐛 | `fix` | Correção de bug |
   | 📝 | `docs` | Documentação e specs |
   | ✅ | `test` | Testes |
   | ♻️ | `refactor` | Refatoração |
   | 🔧 | `chore` | Configuração |
   | 🗃️ | `feat` | Migrations |
   | 🧱 | `build` | Docker, Kubernetes, Helm |
   | 👷 | `ci` | Pipeline |

   Escopos: `identity`, `campanhas`, `worker`, `notificacoes`, `web`, `contracts`, `defaults`, `k8s`, `kong`, `obs`, `ci`, `specs`, `adr`, `runbooks`, `tests`, `scripts`, `claude`, `vscode`, `mcp`.
4. Se houver arquivo `.cs`, `.csproj`, `.props` ou `.slnx` no commit, valide antes:
   ```bash
   dotnet format --verify-no-changes
   dotnet build -c Release -warnaserror
   dotnet test -c Release --no-build --filter Category=Unit
   ```
   Falhou → corrija; não faça commit quebrado.
5. Confira a primeira linha com a regex:
   ```text
   ^\S+ (feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9-]+\))?!?: \S.{0,71}$
   ```
6. Faça o commit adicionando só os arquivos do grupo:
   ```bash
   git add <arquivos>
   git commit -m "<mensagem>"
   ```
   Corpo opcional explica o porquê; rodapé aceita `Refs #n`, `Closes #n` e `BREAKING CHANGE:`.
7. Mostre `git log --oneline -n <quantidade>` ao final.

## Proibido

- Linha `Co-Authored-By` ou qualquer atribuição de autoria no commit.
- `--no-verify`, `--amend` em commit já enviado e `push --force` na `main`.
- Commitar `.env`, `*.pem`, `*.key`, `local.settings.json`, `bin/`, `obj/` ou `coverage/`.
- Push sem pedido explícito do usuário.
