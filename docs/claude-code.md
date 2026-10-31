# Claude Code no projeto

[← README](../README.md)

Como o grupo usa o Claude Code para especificar, implementar e revisar o Conexão Solidária com Spec-Driven Development ([ADR 0017](adr/0017-spec-driven-development.md)).

## Arquivos de configuração

| Arquivo | Versionado | Função |
|---|---|---|
| `CLAUDE.md` | Sim | Memória do projeto: regras, mapa do repositório, comandos; importa a constituição |
| `.claude/settings.json` | Sim | Idioma, permissões (`allow`, `ask`, `deny`), servidores MCP aprovados, atribuição vazia nos commits |
| `.claude/settings.local.json` | Não | Ajustes pessoais (ex.: pasta da LLMWiki como diretório adicional) |
| `.claude/commands/*.md` | Sim | Comandos do fluxo de especificação |
| `.claude/agents/*.md` | Sim | Subagentes com papéis fixos |
| `.claude/skills/<nome>/SKILL.md` | Sim | Procedimentos repetitivos |
| `.mcp.json` | Sim | Servidores MCP do projeto |
| `CLAUDE.local.md` | Não | Anotações pessoais (opcional) |

### `settings.local.json` sugerido

Cada integrante cria o seu, com o caminho da LLMWiki na própria máquina:

```json
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "additionalDirectories": ["C:\\Victor\\Fiap\\LLMWiki\\wiki"],
    "allow": []
  }
}
```

No macOS e no Linux, usar o caminho local, por exemplo `"~/fiap/LLMWiki/wiki"`.

### Permissões

| Lista | Exemplos | Motivo |
|---|---|---|
| `allow` | `dotnet build/test/format`, `git status/diff/log/add/commit`, `kubectl get/describe/logs`, `gh run view` | Leitura e validação frequentes, sem efeito externo |
| `ask` | `git push`, `git rebase`, `kubectl apply/delete`, `helm install`, `docker compose`, `az`, `gh pr create` | Alteram ambiente, histórico ou GitHub |
| `deny` | Ler `.env`, `*.pem`, `*.key`, `local.settings.json`; `push --force`; `--no-verify`; `rm -rf` | Segredos e operações destrutivas |

## Fluxo de Spec-Driven Development

```text
/constitution → /specify → /clarify → /plan → /tasks → /analyze → /implement → reviewer + /code-review → PR
```

| Comando | Entrada | Saída |
|---|---|---|
| `/constitution` | Mudança nos princípios | `specs/constitution.md` com nova versão e documentos afetados atualizados |
| `/specify` | Descrição da funcionalidade | Branch `feat/NNN-nome` e `specs/features/NNN-nome/spec.md` |
| `/clarify` | Feature | Até 5 perguntas; respostas registradas na spec |
| `/plan` | Feature | `research.md`, `data-model.md`, `contracts/`, `plan.md` com a verificação da constituição |
| `/tasks` | Feature | `tasks.md` com tarefas em ordem TDD e mensagem de commit em cada uma |
| `/analyze` | Feature | Relatório de consistência (somente leitura) |
| `/implement` | Feature | Código, testes e um commit por tarefa |

Para saber **o que implementar agora**, abra o [roadmap](../specs/roadmap.md): ele ordena as tarefas de todas as features e da infraestrutura. Use `/implement NNN Tinicial Tfinal` para executar só o trecho da fase atual.

Exemplo de uso para uma funcionalidade nova:

```text
/specify permitir que o gestor exporte a lista de doações de uma campanha em CSV
/clarify
/plan
/tasks
/analyze
/implement
```

## Agentes

| Agente | Papel | Escreve em | Modelo |
|---|---|---|---|
| `architect` | Planeja, pesquisa, escreve contratos e ADRs, confere a constituição | `specs/`, `docs/adr/` | opus |
| `test-writer` | Escreve os testes primeiro (unidade, integração, contrato, BDD, bUnit, k6) e eleva a cobertura | `tests/` | herdado |
| `implementer` | Escreve o mínimo de código para os testes passarem | `src/`, `deploy/` | herdado |
| `reviewer` | Revisa o diff contra spec, constituição e padrões; não edita | — | opus |

## Skills

| Skill | Quando usar |
|---|---|
| `commit-gitmoji` | Todo commit: agrupa por arquivo, valida build e mensagem, sem coautoria |
| `nova-adr` | Decisão arquitetural nova ou substituição de ADR |
| `verificar-cobertura` | Fim de fase do `/implement` e antes de PR (mínimo 90%) |
| `documentar-xml` | Depois de escrever C#: `/// <summary>` em todo tipo e membro |
| `nova-migration` | Mudança em entidades ou tabelas do EF Core |
| `validar-k8s` | Antes de commitar manifestos em `deploy/k8s/` |

Skills do próprio Claude Code também usadas:

| Skill | Quando usar |
|---|---|
| `/code-review` | Fim de cada feature, antes do PR |
| `/security-review` | Features 001, 003 e 008, infraestrutura e revisão final (Etapa 16) |
| `/simplify` | Depois do `/implement`, para remover duplicação sem mudar comportamento |
| `/run` | Subir a stack e conferir o fluxo de doação de ponta a ponta |
| `/loop` | Acompanhar a execução do CI até terminar |
| `/fewer-permission-prompts` | Depois de algumas sessões, para liberar leituras recorrentes |
| `pdf` | Gerar PDF de documentos, se o grupo quiser (não é exigido na entrega) |

## Servidores MCP (`.mcp.json`)

| Servidor | Tipo | Uso | Requisito |
|---|---|---|---|
| `microsoft-learn` | HTTP | Documentação oficial e atual de .NET 10, ASP.NET Core, EF Core, Blazor, Azure Functions e AKS | Nenhum |
| `github` | HTTP | Issues, Projects, PRs, status do CI, releases | Variável de ambiente `GITHUB_PERSONAL_ACCESS_TOKEN` com escopo mínimo (repo e project) |
| `playwright` | stdio (`npx`) | Inspecionar e testar as telas do Blazor; apoiar os testes E2E | Node.js LTS |
| `claude-vscode` | Embutido na extensão | `getDiagnostics`: erros e avisos de compilação antes do commit | Extensão do VS Code |

Na primeira sessão, o Claude Code pede aprovação dos servidores do `.mcp.json`; os três estão pré-aprovados em `enabledMcpjsonServers`. Conferir com `/mcp`.

No Windows nativo, se o `npx` não iniciar pelo `.mcp.json`, registrar o Playwright no escopo local:

```bash
claude mcp add playwright -s local -- cmd /c npx -y @playwright/mcp@latest
```

O token do GitHub nunca vai para o repositório: definir `GITHUB_PERSONAL_ACCESS_TOKEN` no ambiente do sistema ou usar o `gh` CLI autenticado como alternativa.

## Regras de uso

1. A LLMWiki (`C:\Victor\Fiap\LLMWiki\wiki`) é **somente leitura**: consultar, nunca alterar.
2. Toda mudança gerada passa pelos mesmos hooks, CI e revisão de PR que o código escrito à mão.
3. Comandos destrutivos ou com efeito externo (push, PR, `kubectl apply/delete`, `az`) só com confirmação de um integrante.
4. Nenhum segredo ou dado pessoal real em prompts.
5. Respostas em português do Brasil (`language` no `settings.json`).

## Uso por etapa do plano

| Etapa | Comandos e skills | MCP |
|---|---|---|
| 1 Fundação | `commit-gitmoji` | GitHub (criar repositório) |
| 2 Planejamento | `/specify`, `/clarify` para cada feature; `nova-adr` | GitHub (issues e Project) |
| 3 Esqueleto e feature 009 | `/analyze 009`, `/implement 009`, `verificar-cobertura`, `commit-gitmoji`, `/init` (revisar o `CLAUDE.md`), `/fewer-permission-prompts` | Microsoft Learn, `claude-vscode`, GitHub |
| 4–8 Features (001–008) | `/plan`, `/tasks`, `/analyze`, `/implement`, `reviewer`, `/code-review`, `/security-review` | Microsoft Learn, `claude-vscode`, Playwright |
| 10–13 Infra e observabilidade | `validar-k8s`, `/run`, `/code-review` | Microsoft Learn |
| 14 CI/CD | `/loop`, `/code-review` | GitHub |
| 15 Azure | `/security-review` | Microsoft Learn |
| 16 Segurança | `/security-review` | — |
| 17 Documentação | `pdf` | — |
| 18–19 Vídeo e conclusão | — | GitHub (release) |
