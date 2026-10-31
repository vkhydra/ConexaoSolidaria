# CLAUDE.md — Conexão Solidária

Plataforma de doações da ONG Esperança Solidária: Hackathon da Fase 5 da PósTech FIAP (Arquitetura de Sistemas .NET). Monorepo .NET 10 com microsserviços, mensageria, Kubernetes local e observabilidade.

## Como responder

- Sempre em **português do Brasil**, com acentuação correta.
- Objetivo, claro e direto; explique passo a passo quando a tarefa tiver etapas.
- Em caso de dúvida, **pergunte** antes de agir. Não dê informações que não foram pedidas.

## Regras inegociáveis

A constituição prevalece sobre qualquer outro documento:

@specs/constitution.md

Resumo prático:

1. **Spec antes do código:** toda funcionalidade passa por `/specify` → `/clarify` → `/plan` → `/tasks` → `/analyze` → `/implement`.
2. **Commits:** `<gitmoji> <tipo>(<escopo>): <descrição>` em português; **um arquivo por commit** (ou o menor conjunto que compila); todo commit compila e passa nos testes; **sem `Co-Authored-By`**; nunca `--no-verify`. Use a skill `commit-gitmoji`.
3. **Testes:** TDD no domínio; cobertura **≥ 90% de linhas e ramos**, no total e por projeto (skill `verificar-cobertura`).
4. **Documentação no código:** `/// <summary>` em português em **todo** tipo e membro C#, inclusive testes (skill `documentar-xml`).
5. **Segurança e LGPD:** nenhum dado pessoal em logs e eventos; nenhum segredo em código, YAML ou prompt.
6. **Decisões:** decisão arquitetural nova vira ADR (skill `nova-adr`).
7. **LLMWiki** em `C:\Victor\Fiap\LLMWiki\wiki` é **somente leitura**: consulte, nunca altere.
8. **Push, PR, `kubectl apply/delete`, `helm`, `az`:** só com pedido explícito do usuário.

## Mapa do repositório

| Caminho | Conteúdo |
|---|---|
| `specs/constitution.md` | Princípios do projeto |
| `specs/product/` | Visão do produto, requisitos não funcionais, domínio, arquitetura, API e eventos, dados, segurança, observabilidade, infraestrutura, CI/CD |
| `specs/features/NNN-nome/` | `spec.md`, `research.md`, `plan.md`, `data-model.md`, `contracts/`, `tasks.md` de cada funcionalidade |
| `docs/` | Pré-requisitos, processo ágil, testes, padrões de código, git, plano de execução, entrega, manuais, glossário |
| `docs/adr/` | Decisões de arquitetura |
| `docs/runbooks/` | Resposta a alertas e operações |
| `src/` | Código (`BuildingBlocks`, `Services/Identity`, `Services/Campanhas`, `Functions`, `Web`) |
| `tests/` | `unit/`, `integration/`, `contract/`, `e2e/` |
| `deploy/` | docker-compose, Kustomize, Helm values, Kong, observabilidade, Bicep |
| `scripts/` | Automação em PowerShell 7 (`pwsh`), igual em Windows, macOS e Linux |
| `.claude/` | Comandos, agentes e skills do Claude Code |

## Features

| # | Feature | Serviço |
|---|---|---|
| 001 | [Autenticação e autorização](specs/features/001-autenticacao/spec.md) | Identity.Api |
| 002 | [Gestão de campanhas](specs/features/002-gestao-campanhas/spec.md) | Campanhas.Api |
| 003 | [Cadastro de doador](specs/features/003-cadastro-doador/spec.md) | Identity.Api |
| 004 | [Painel de transparência](specs/features/004-painel-transparencia/spec.md) | Campanhas.Api |
| 005 | [Doação assíncrona](specs/features/005-doacao/spec.md) | Campanhas.Api + Arrecadacao.Worker |
| 006 | [Busca de campanhas](specs/features/006-busca-campanhas/spec.md) | Campanhas.Api |
| 007 | [Recibo de doação](specs/features/007-recibo-doacao/spec.md) | Notificacoes.Function |
| 008 | [Direitos do titular](specs/features/008-direitos-titular/spec.md) | Identity.Api |

## Comandos

```bash
dotnet tool restore                                   # dotnet-ef, husky, reportgenerator
dotnet build -c Release -warnaserror                  # build (avisos são erro)
dotnet format --verify-no-changes                     # formato
dotnet test -c Release --filter Category=Unit         # testes de unidade
dotnet test -c Release                                # todos os testes (exige Docker)
pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90 -ExecutarTestes
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
kubectl apply -k deploy/k8s/overlays/local --dry-run=server
```

## Fluxo com o Claude Code

| Etapa | Ferramenta |
|---|---|
| Princípios | `/constitution` |
| Especificar, esclarecer, planejar, quebrar em tarefas | `/specify`, `/clarify`, `/plan`, `/tasks` |
| Conferir consistência | `/analyze` |
| Implementar | `/implement` (usa os agentes `test-writer` e `implementer`) |
| Revisar | agente `reviewer`, `/code-review`, `/security-review` |
| Apoio | skills `commit-gitmoji`, `nova-adr`, `verificar-cobertura`, `documentar-xml`, `nova-migration`, `validar-k8s` |
| Documentação atual | MCP `microsoft-learn`; GitHub pelo MCP `github` ou `gh` |

Detalhes: [docs/claude-code.md](docs/claude-code.md).

## Convenções de código (resumo)

- Camadas: `Api` → `Application` → `Domain`; `Infrastructure` implementa interfaces de `Application`. `Domain` não referencia infraestrutura.
- Casos de uso com `ExecutarAsync(request, CancellationToken)`; erros de negócio como `Result` com código estável.
- Datas por `TimeProvider`, fuso `America/Sao_Paulo`; dinheiro em `decimal` com 2 casas.
- Pacotes proibidos: MediatR, AutoMapper, FluentAssertions, MassTransit 9+, Moq.
- Completo em [docs/padroes-de-codigo.md](docs/padroes-de-codigo.md).
