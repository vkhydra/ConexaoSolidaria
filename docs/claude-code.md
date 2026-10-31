# 19 — Skills e MCP do Claude Code

[← README](../README.md)

Como o Claude Code apoia cada etapa. **Skills** são comandos com instruções prontas (`/nome`). **MCP** (Model Context Protocol) conecta o Claude Code a ferramentas externas.

## 19.1 Regras de uso

1. O Claude Code segue o `CLAUDE.md` do repositório (criado na Etapa 3), que aponta para esta spec e para a LLMWiki.
2. A LLMWiki (`C:\Victor\Fiap\LLMWiki\wiki`) é **somente leitura**: consultar, nunca alterar.
3. Toda mudança gerada passa pelos mesmos hooks, CI e revisão de PR que o código escrito à mão.
4. Comandos destrutivos (apagar recursos da Azure, `kubectl delete`, `git push --force`) só com confirmação explícita de um integrante.
5. Nenhum segredo é colado em prompts.

## 19.2 Skills

| Skill | Etapas | Quando usar | Exemplo de pedido |
|---|---|---|---|
| `/init` | 3 | Depois de criar a solução, para gerar o `CLAUDE.md` | `/init` e depois: "inclua a regra de commits da spec 17, os comandos de build e teste e a LLMWiki como referência somente leitura" |
| `/fewer-permission-prompts` | 3 em diante | Após algumas sessões, para liberar comandos de leitura recorrentes (`dotnet build`, `kubectl get`) | `/fewer-permission-prompts` |
| `/code-review` | Fim de cada etapa | Antes de abrir o PR | `/code-review` |
| `/security-review` | 4, 5, 11, 16 | Autenticação, LGPD, segredos, revisão final | `/security-review` |
| `/simplify` | Fim de cada etapa | Remover duplicação e complexidade sem mudar comportamento | `/simplify` |
| `/run` | 10–13 | Subir a stack e validar o fluxo de doação de ponta a ponta | `/run` "suba o docker compose e faça uma doação de teste" |
| `/loop` | 14 | Acompanhar a execução do CI até terminar | `/loop` "verifique o último run do CI com gh run view" |
| `pdf` | 17 | Gerar o PDF de justificativa dos bancos e o relatório de entrega | "gere o PDF a partir de docs/justificativa-bancos.md" |
| `dataviz` | 13 (opcional) | Revisar cores, unidades e legibilidade do dashboard Grafana | "revise os painéis do dashboard conexao-solidaria.json" |
| `artifact-diagramming` (opcional) | 2, 17 | Diagramas explicativos para o vídeo | "desenhe o fluxo da doação com Outbox" |

## 19.3 MCP

| Servidor | Etapas | Uso | Configuração |
|---|---|---|---|
| `claude-vscode` (embutido na extensão) | Todas | `getDiagnostics`: conferir erros e avisos de compilação antes de cada commit | Já disponível na extensão do VS Code |
| Microsoft Learn | 3–15 | Documentação oficial e atual de .NET 10, ASP.NET Core, EF Core, Blazor, Azure Functions, AKS | `claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp` |
| GitHub | 1, 2, 14, 19 | Issues, GitHub Projects, PRs, status do CI, releases | Servidor oficial do GitHub via `/mcp`; alternativa: `gh` CLI |
| Playwright | 8, 9 | Testar e inspecionar as telas do Blazor; apoiar os testes E2E | `claude mcp add playwright -- npx @playwright/mcp@latest` |
| Claude Docs (opcional) | 17 | Escrever o relatório de entrega como documento compartilhável | Conector do claude.ai |

Verificação: `/mcp` lista os servidores conectados e o status de cada um.

## 19.4 Uso por etapa (resumo)

| Etapa | Skills | MCP |
|---|---|---|
| 1 Fundação | — | GitHub (criar repositório) |
| 2 Planejamento | `artifact-diagramming` (opcional) | GitHub (issues e Project) |
| 3 Esqueleto | `/init`, `/fewer-permission-prompts` | Microsoft Learn, `claude-vscode` |
| 4 Identity | `/security-review`, `/code-review`, `/simplify` | Microsoft Learn, `claude-vscode` |
| 5 Campanhas | `/security-review`, `/code-review`, `/simplify` | Microsoft Learn, `claude-vscode` |
| 6 Worker | `/code-review`, `/simplify` | Microsoft Learn, `claude-vscode` |
| 7 Função | `/code-review` | Microsoft Learn |
| 8 Blazor | `/code-review` | Playwright, Microsoft Learn |
| 9 Testes | `/code-review` | Playwright |
| 10–13 Infra e observabilidade | `/run`, `/code-review`, `dataviz` (opcional) | Microsoft Learn |
| 14 CI/CD | `/loop`, `/code-review` | GitHub |
| 15 Azure | `/security-review` | Microsoft Learn |
| 16 Segurança | `/security-review` | — |
| 17 Documentação | `pdf` | Claude Docs (opcional) |
| 18–19 Vídeo e conclusão | — | GitHub (release) |
