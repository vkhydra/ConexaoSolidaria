# Especificação — Conexão Solidária

Especificação do Hackathon da Fase 5 (PósTech FIAP — Arquitetura de Sistemas .NET), dos pré-requisitos até a conclusão. O conteúdo está dividido em arquivos temáticos em [`docs/spec/`](spec/).

| Item | Valor |
|---|---|
| Enunciado | Hackathon Fase 5 — "Conexão Solidária" (ONG Esperança Solidária) |
| Base de conhecimento | LLMWiki em `C:\Victor\Fiap\LLMWiki\wiki` (somente leitura), citada como `wiki:<caminho>` |
| Stack | .NET 10, PostgreSQL, MongoDB, Redis, Elasticsearch, RabbitMQ + MassTransit, Blazor |
| Execução | Local (Minikube), com etapa de cloud **opcional** na Azure |
| CI/CD | GitHub Actions + GHCR |
| Branch principal | `main` |

## Índice

### Contexto e processo
| # | Arquivo | Conteúdo |
|---|---|---|
| 01 | [Visão geral](spec/01-visao-geral.md) | Contexto, objetivos, personas, escopo, premissas, linguagem ubíqua |
| 02 | [Pré-requisitos](spec/02-pre-requisitos.md) | Hardware, software, configuração do ambiente, contas, leitura prévia |
| 03 | [Processo ágil](spec/03-processo-agil.md) | Scrum, Kanban, XP, sprints, DoR, DoD, backlog inicial |

### Requisitos
| # | Arquivo | Conteúdo |
|---|---|---|
| 04 | [Requisitos funcionais](spec/04-requisitos-funcionais.md) | RF01–RF08 com regras, validações e critérios Gherkin |
| 05 | [Requisitos não funcionais](spec/05-requisitos-nao-funcionais.md) | Desempenho, escala, resiliência, segurança, SLOs |

### Projeto técnico
| # | Arquivo | Conteúdo |
|---|---|---|
| 06 | [Modelo de domínio](spec/06-modelo-de-dominio.md) | Subdomínios, bounded contexts, agregados, value objects, eventos |
| 07 | [Arquitetura](spec/07-arquitetura.md) | Diagramas, componentes, fluxos, estrutura de pastas, configuração |
| 08 | [Decisões arquiteturais](spec/08-decisoes-arquiteturais.md) | ADRs 0001–0016 |
| 09 | [API e eventos](spec/09-api-e-eventos.md) | Endpoints com exemplos, erros, contratos de eventos, topologia do RabbitMQ |
| 10 | [Dados](spec/10-dados.md) | Esquemas, índices, consultas, cache, Elasticsearch, justificativa dos bancos |
| 11 | [Segurança e LGPD](spec/11-seguranca-e-lgpd.md) | Autenticação, autorização, dados pessoais, segredos, OWASP |
| 12 | [Observabilidade](spec/12-observabilidade.md) | Health checks, métricas, dashboard, alertas, traces, logs, Zabbix |
| 13 | [Infraestrutura](spec/13-infraestrutura.md) | Docker, docker-compose, Kubernetes, Kong, KEDA, Azure opcional |
| 14 | [CI/CD](spec/14-ci-cd.md) | Workflows, segurança do pipeline, proteção da `main`, releases |
| 15 | [Testes](spec/15-testes.md) | Pirâmide, TDD, BDD, integração, carga, cobertura |

### Padrões e apoio
| # | Arquivo | Conteúdo |
|---|---|---|
| 16 | [Padrões de código](spec/16-padroes-de-codigo.md) | Configuração da solução, pacotes, SOLID, regras, revisão |
| 17 | [Git e commits](spec/17-git-e-commits.md) | Commits semânticos com gitmoji, hooks, branches, PRs |
| 18 | [Cobertura das fases](spec/18-cobertura-das-fases.md) | Onde cada disciplina das 5 fases é aplicada |
| 19 | [Skills e MCP](spec/19-skills-e-mcp.md) | Uso do Claude Code por etapa |

### Execução e entrega
| # | Arquivo | Conteúdo |
|---|---|---|
| 20 | [Plano de execução](spec/20-plano-de-execucao.md) | Etapas 0–19 com tarefas, commits e critérios de pronto |
| 21 | [Entrega e riscos](spec/21-entrega-e-riscos.md) | Checklist do enunciado, relatório, riscos, ordem de corte |
| — | [Roteiro do vídeo](ROTEIRO-VIDEO.md) | Apresentação de até 15 minutos |

## Como ler

- **Para começar o trabalho:** 01 → 02 → 20.
- **Para implementar uma etapa:** a etapa em 20 aponta os arquivos técnicos necessários.
- **Para revisar um PR:** 16 e 17.
- **Antes de entregar:** 21 e o roteiro do vídeo.
