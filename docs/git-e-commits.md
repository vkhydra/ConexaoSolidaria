# Git e commits

[← README](../README.md)

Regras obrigatórias: **todo arquivo criado tem commit semântico com gitmoji** e **todo commit compila sem erro**. Referências: `wiki:conceitos/pull-request`, `wiki:conceitos/integracao-continua`.

## Formato da mensagem

```text
<gitmoji> <tipo>(<escopo opcional>)<! opcional>: <descrição>

<corpo opcional: o porquê da mudança, quebrado em 72 colunas>

<rodapé opcional>
```

| Parte | Regra |
|---|---|
| Gitmoji | Emoji Unicode (não `:codigo:`), conforme a tabela 17.2 |
| Tipo | `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert` |
| Escopo | Opcional, minúsculo: `identity`, `campanhas`, `worker`, `notificacoes`, `web`, `contracts`, `defaults`, `k8s`, `kong`, `obs`, `ci`, `specs`, `adr`, `runbooks`, `ddd`, `agil`, `video`, `tests`, `bdd`, `e2e`, `load`, `lgpd`, `azure`, `scripts`, `claude`, `vscode`, `mcp` |
| `!` | Mudança incompatível (contrato de API ou evento) |
| Descrição | Imperativo, em português, minúscula, sem ponto final, até 72 caracteres |
| Rodapé | `Refs #12`, `Closes #12`, `BREAKING CHANGE: ...`. **Sem** linha de coautoria (`Co-Authored-By`) |

## Gitmojis permitidos

O **gitmoji** indica o assunto da mudança e precisa estar nesta tabela (o hook rejeita os demais). O **tipo** segue o Conventional Commits pelo efeito da mudança. A coluna "Tipo usual" é o caso mais comum, mas o tipo muda quando o arquivo alterado pede outro:

- só documentação ou specs → `docs` (ex.: `🚚 docs: move ...`, `🔥 docs: remove ...`, `🐛 docs: corrige regra ...`);
- infraestrutura (Docker, Kubernetes, Helm) → `build` (ex.: `📈 build(obs): adiciona service monitors`, `🔒️ build(k8s): adiciona network policies`);
- configuração de ferramentas → `chore` (ex.: `✏️ chore(claude): ...`);
- código que muda comportamento → `feat` ou `fix` (ex.: `🔒️ feat(identity): adiciona cifragem do cpf`, `⚡️ feat(campanhas): adiciona cache do painel`).

| Gitmoji | Tipo usual | Quando |
|---|---|---|
| 🎉 | `chore` | Criar a solução ou um projeto novo |
| ✨ | `feat` | Nova funcionalidade |
| 🐛 | `fix` | Correção de bug |
| 🚑️ | `fix` | Correção urgente na `main` |
| 📝 | `docs` | Documentação |
| ✏️ | `docs` | Correção de texto ou digitação |
| 💡 | `docs` | Comentários no código |
| ✅ | `test` | Adicionar ou ajustar testes |
| ♻️ | `refactor` | Refatoração sem mudar comportamento |
| 🏗️ | `refactor` | Mudança arquitetural |
| 🚚 | `refactor` | Mover ou renomear arquivos |
| 🔥 | `refactor` | Remover código ou arquivos |
| ⚡️ | `perf` | Desempenho |
| 🎨 | `style` | Formatação e estrutura |
| 🚨 | `style` | Corrigir avisos de analisador |
| 🔧 | `chore` | Arquivos de configuração |
| 🙈 | `chore` | `.gitignore` e `.dockerignore` |
| 🔖 | `chore` | Release / tag |
| ➕ | `build` | Adicionar dependência |
| ➖ | `build` | Remover dependência |
| ⬆️ | `build` | Atualizar dependência |
| 📌 | `build` | Fixar versão de dependência |
| 🧱 | `build` | Infraestrutura (Docker, Kubernetes, Kong, Helm, Bicep) |
| 👷 | `ci` | Criar ou alterar pipeline |
| 💚 | `ci` | Corrigir pipeline quebrado |
| 🗃️ | `feat` | Banco de dados, migrations |
| 🛂 | `feat` | Autenticação e autorização |
| 🔒️ | `fix` | Segurança ou privacidade |
| 🔐 | `chore` | Modelos de segredos (`*.example`) |
| 🦺 | `feat` | Validações |
| 🥅 | `feat` | Tratamento de erros |
| 🔊 | `feat` | Logs |
| 📈 | `feat` | Métricas, dashboards, alertas |
| 🩺 | `feat` | Health checks |
| 💄 | `feat` | Interface (Blazor, CSS) |
| 🌱 | `chore` | Dados de seed |
| 🏷️ | `feat` | Tipos e contratos (eventos, DTOs) |
| 🤖 | `feat` | Funcionalidade de IA (bônus) |
| ⏪️ | `revert` | Reverter commit |
| 💥 | `feat!` | Mudança incompatível |

Exemplos:
```text
🎉 chore: cria solução ConexaoSolidaria
🔧 chore: adiciona Directory.Build.props com avisos como erro
🏷️ feat(contracts): adiciona DoacaoRecebidaEvent v1
✨ feat(campanhas): adiciona criação de campanha
🗃️ feat(campanhas): adiciona migration inicial de campanhas
🛂 feat(identity): emite jwt rs256 no login
🧱 build(k8s): adiciona deployment da campanhas-api
👷 ci: gera imagem docker no push da main
🐛 fix(worker): evita soma duplicada em reentrega
```

## Granularidade

1. **Um commit por arquivo criado** sempre que o arquivo compila e passa nos testes sozinho (documentação, configuração, manifestos, testes de classes já existentes).
2. Quando um arquivo depende de outro para compilar (`.csproj` + `Program.cs`; interface + implementação + registro no DI; entidade + configuração EF + migration), o commit leva o **menor conjunto que compila e passa nos testes**, e a mensagem descreve a unidade lógica.
3. Teste e implementação mínima que o faz passar vão juntos; refatoração em commit separado.
4. Nunca misturar mudanças não relacionadas; nunca commitar código comentado ou arquivos gerados (`bin/`, `obj/`).

## Garantia de que todo commit compila

### Camada 1 — Hooks locais (Husky.Net)

Instalação (Etapa 3): `dotnet new tool-manifest`, `dotnet tool install Husky`, `dotnet husky install`. Os hooks ficam versionados em `.husky/` e são ativados por quem roda `dotnet tool restore` + `dotnet husky install`. Um `Target` no `Directory.Build.props` roda `dotnet husky install` no primeiro restore, para ninguém esquecer.

`.husky/task-runner.json`:
```json
{
  "tasks": [
    {
      "name": "formato",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["format", "--verify-no-changes"]
    },
    {
      "name": "build",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["build", "-c", "Release", "-warnaserror"]
    },
    {
      "name": "testes-unidade",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["test", "-c", "Release", "--no-build", "--filter", "Category=Unit"]
    },
    {
      "name": "mensagem",
      "group": "commit-msg",
      "command": "pwsh",
      "args": ["-NoProfile", "-File", "scripts/validar-mensagem-commit.ps1", "${args}"]
    },
    {
      "name": "testes-e-cobertura",
      "group": "pre-push",
      "command": "pwsh",
      "args": ["-NoProfile", "-File", "scripts/verificar-cobertura.ps1", "-Minimo", "90", "-ExecutarTestes"]
    }
  ]
}
```

`scripts/validar-mensagem-commit.ps1` valida a primeira linha com a expressão regular:
```text
^\S+ (feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9-]+\))?!?: \S.{0,71}$
```
e confere se o primeiro símbolo está na lista da tabela 17.2. Commits gerados pelo git (`Merge`, `Revert`) são aceitos.

No `pre-push`, `scripts/verificar-cobertura.ps1 -ExecutarTestes` roda todos os testes com cobertura, gera o relatório e bloqueia o push se a cobertura ficar abaixo de 90% ([estratégia de testes](testes.md)).

### Camada 2 — Verificação de cada commit antes do PR
```powershell
git fetch origin
git rebase --exec "dotnet build -c Release -warnaserror && dotnet test -c Release --no-build" origin/main
```
O rebase para no primeiro commit que falhar; corrigir com `git commit --amend` e `git rebase --continue`.

### Camada 3 — CI
O CI roda em todo push e PR ([CI/CD](../specs/product/ci-cd.md)). A `main` é protegida e aceita merge só com CI verde.

### Proibido
- `git commit --no-verify` e `git push --no-verify`.
- `git push --force` na `main`.

## Branches

| Branch | Uso | Vida |
|---|---|---|
| `main` | Sempre implantável; protegida | Permanente |
| `feat/<issue>-<descricao>` | Funcionalidade | Até 2 dias |
| `fix/<issue>-<descricao>` | Correção | Até 1 dia |
| `docs/<descricao>`, `ci/<descricao>`, `infra/<descricao>` | Outros | Até 2 dias |

Fluxo *trunk-based*: branch curta a partir da `main` atualizada → commits atômicos → `git rebase origin/main` → PR → revisão + CI → merge por **rebase** → apagar a branch.

## Pull requests

Modelo em `.github/pull_request_template.md`:
```markdown
## O que muda
## Por quê
Refs #
## Como testar
## Checklist
- [ ] Commits no padrão e compilando (`git rebase --exec`)
- [ ] Testes novos/ajustados
- [ ] Sem dados pessoais em logs e eventos
- [ ] Documentação/ADR atualizados
- [ ] `/code-review` executado
```

## Histórico já existente

Os commits de especificação e de configuração (Etapa 1) seguem este padrão e só alteram documentação e configuração. Os commits anteriores à decisão de não usar coautoria mantêm a linha `Co-Authored-By`; o histórico não é reescrito.

## Commits feitos com o Claude Code

- A skill `commit-gitmoji` (`.claude/skills/commit-gitmoji/SKILL.md`) aplica estas regras.
- `.claude/settings.json` define `attribution` vazio, para que nenhum commit ou PR receba linha de coautoria.
- O comando `/implement` faz um commit por tarefa do `tasks.md`, com a mensagem já escrita na tarefa.
