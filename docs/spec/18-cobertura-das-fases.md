# 18 — Cobertura das 5 fases

[← README](../../README.md) · Anterior: [17 — Git e commits](17-git-e-commits.md) · Próximo: [19 — Skills e MCP](19-skills-e-mcp.md)

Onde cada disciplina do curso é aplicada. Coluna **Etapa** aponta para o [plano de execução](20-plano-de-execucao.md); **Wiki** aponta para `C:\Victor\Fiap\LLMWiki\wiki`.

Legenda de status: **Obrigatório** (enunciado), **Aplicado** (escolha do projeto para cobrir a fase), **Bônus** (se houver tempo), **Alternativa** (registrada em ADR, não implementada).

## Fase 1 — Arquitetura e Qualidade de Software

| Disciplina | Conteúdo | Aplicação no projeto | Status | Etapa | Wiki |
|---|---|---|---|---|---|
| APIs .NET | API REST, verbos e status | Endpoints da [seção 09](../../specs/product/api-e-eventos.md) | Obrigatório | 4, 5 | `disciplinas/api-dotnet` |
| | Middlewares e DI | `IExceptionHandler`, correlação, DI por construtor | Aplicado | 3–5 | `conceitos/middleware-aspnet-core` |
| | Logs | Serilog estruturado | Aplicado | 3 | `conceitos/logging-dotnet` |
| | Serialização | `System.Text.Json`, enums como texto, contratos de evento | Aplicado | 4–6 | `conceitos/serializacao-de-dados` |
| | Autenticação e autorização | JWT RS256, policies por role | Obrigatório | 4 | `conceitos/jwt` |
| | Dapper | Consulta do painel | Aplicado | 5 | `conceitos/dapper` |
| | Documentação | OpenAPI + Swagger UI | Aplicado | 4, 5 | `conceitos/openapi-swagger` |
| | Cache | `HybridCache` + Redis | Aplicado | 5 | `conceitos/cache-de-api` |
| | Minimal API | Todas as APIs | Aplicado | 4, 5 | `conceitos/minimal-api` |
| | GraphQL | Consulta do painel com HotChocolate (`/graphql`, só leitura, profundidade limitada) | Bônus | 9 | `conceitos/graphql` |
| | gRPC | Chamada interna Web → Campanhas para o painel | Bônus | 9 | `conceitos/grpc` |
| | Resiliência | `Microsoft.Extensions.Http.Resilience`; retry do MassTransit | Aplicado | 5, 6, 8 | `conceitos/resiliencia-em-apis` |
| DDD | Estratégico | Subdomínios, bounded contexts, context map | Aplicado | 2 | `disciplinas/ddd` |
| | Tático | Agregados, VOs, eventos de domínio, repositórios | Aplicado | 4, 5 | `conceitos/aggregate`, `conceitos/value-object` |
| | Event Storming | Fluxos "criar campanha" e "doar" no Miro | Aplicado | 2 | `conceitos/event-storming` |
| | Domain Storytelling | História da doação | Aplicado | 2 | `conceitos/domain-storytelling` |
| | Linguagem ubíqua | Glossário da [seção 01](../../specs/product/overview.md) | Aplicado | 2 | `conceitos/linguagem-ubiqua` |
| | Requisitos, DoR, DoD | [Seção 03](03-processo-agil.md) | Aplicado | 2 | `conceitos/definition-of-ready` |
| EF Core | Code First, Fluent API | `IEntityTypeConfiguration<T>` | Aplicado | 4, 5 | `conceitos/code-first` |
| | Migrations | Migration bundle em Job | Aplicado | 4, 5, 11 | `conceitos/migrations` |
| | Performance | `AsNoTracking`, projeção, índices | Aplicado | 5 | `conceitos/otimizacao-de-performance-ef-core` |
| | Concorrência | `xmin` + `If-Match` | Aplicado | 5 | `boas-praticas/efcore` |
| Princípios de qualidade | Pirâmide de testes | [Seção 15](15-testes.md) | Aplicado | 4–9 | `conceitos/piramide-de-testes` |
| | Testes de UI | bUnit; Playwright | Aplicado / Bônus | 8 | `conceitos/automacao-de-testes-ui` |
| Qualidade de software | Testes de unidade | xUnit | Bônus do enunciado | 4–6 | `conceitos/teste-de-unidade` |
| | TDD | Domínio | Aplicado | 4, 5 | `conceitos/tdd` |
| | BDD | Reqnroll + Gherkin | Aplicado | 5, 9 | `conceitos/bdd` |
| | Integração | Testcontainers | Aplicado | 4–6 | `conceitos/teste-de-integracao` |
| | Carga | k6 | Aplicado | 9 | `conceitos/k6` |

## Fase 2 — Microsserviços e Orquestração de Containers

| Disciplina | Conteúdo | Aplicação no projeto | Status | Etapa | Wiki |
|---|---|---|---|---|---|
| Microsserviços | Decomposição | 4 componentes de backend por contexto | Obrigatório | 3 | `disciplinas/microsservicos` |
| | Banco por serviço | Bancos por contexto, usuários separados | Aplicado | 4, 5 | `conceitos/banco-de-dados-por-servico` |
| | Comunicação assíncrona | Eventos via RabbitMQ | Obrigatório | 6 | `conceitos/comunicacao-sincrona-e-assincrona` |
| | Resiliência | Circuit breaker, retry, bulkhead (prefetch) | Aplicado | 6, 8 | `conceitos/circuit-breaker` |
| | Saga | Não necessária (um passo de negócio); registrado em ADR | Alternativa | 2 | `conceitos/saga` |
| | Segurança | JWT validado em cada serviço; NetworkPolicy | Aplicado | 4, 11 | `conceitos/seguranca-em-microsservicos` |
| Docker | Dockerfile multi-stage | Um por serviço | Aplicado | 10 | `conceitos/dockerfile` |
| | Compose | Stack completa | Aplicado | 10 | `conceitos/docker-compose` |
| | Segurança de imagens | Não root, Trivy | Aplicado | 10, 14 | `conceitos/seguranca-de-imagens-docker` |
| Kubernetes | Deployments, Services, ConfigMaps | Manifestos `.yaml` | Obrigatório | 11 | `disciplinas/kubernetes` |
| | Secrets | Gerados por script | Aplicado | 11 | `conceitos/configmap` |
| | Probes | live, ready, startup | Aplicado | 11 | `conceitos/probes-kubernetes` |
| | HPA | APIs | Aplicado | 11 | `conceitos/hpa` |
| | Volumes | PVC nos StatefulSets | Aplicado | 11 | `conceitos/persistent-volume-e-pvc` |
| Orquestração (AKS) | AKS e CI/CD com AKS | Etapa opcional | Aplicado (opcional) | 15 | `disciplinas/orquestracao-k8s` |
| Mensageria | RabbitMQ, exchanges | Fanout por evento | Obrigatório | 6 | `conceitos/rabbitmq` |
| | MassTransit | Outbox, retry, DLQ | Aplicado | 5, 6 | `conceitos/masstransit` |
| | Garantias e idempotência | At-least-once + idempotência | Aplicado | 6 | `conceitos/garantias-de-entrega` |
| | Kafka / CDC | Kafka e Debezium não usados | Alternativa | — | `conceitos/apache-kafka` |

## Fase 3 — Serverless e Monitoramento

| Disciplina | Conteúdo | Aplicação no projeto | Status | Etapa | Wiki |
|---|---|---|---|---|---|
| Serverless | FaaS orientado a evento | `Notificacoes.Function` + KEDA | Aplicado | 7 | `conceitos/azure-functions` |
| | AWS Lambda / Step Functions | Não usados (Azure escolhida) | Alternativa | — | `conceitos/aws-lambda` |
| API Gateway | Kong | Rotas, rate limiting, CORS, métricas | Bônus do enunciado | 12 | `conceitos/kong-gateway` |
| | Azure APIM | Não usado (pago) | Alternativa | — | `conceitos/azure-api-management` |
| Monitoramento | Prometheus | Coleta de `/metrics` | Obrigatório | 13 | `conceitos/prometheus` |
| | Grafana | Dashboard como código | Obrigatório | 13 | `conceitos/grafana` |
| | Zabbix | Cenário web em `/health/ready` | Bônus | 13 | `conceitos/zabbix` |
| | Alertmanager | Alertas da seção 12 | Aplicado | 13 | `conceitos/alertmanager` |
| Monitoramento avançado | Tracing distribuído | OpenTelemetry + Jaeger | Aplicado | 3, 13 | `conceitos/distributed-tracing` |
| | Golden metrics, SLO | Dashboard e [seção 05](../../specs/product/requisitos-nao-funcionais.md) | Aplicado | 13 | `conceitos/golden-metrics` |
| | Logs e severidade | Serilog | Aplicado | 3 | `conceitos/logs-e-severidade` |
| | Datadog / New Relic | Não usados (SaaS) | Alternativa | — | `conceitos/apm` |
| Arquitetura (TC Fase 3) | Event sourcing ou audit log | Livro de doações no MongoDB | Aplicado | 6 | `conceitos/event-sourcing` |

## Fase 4 — Cloud, DevOps e Elasticsearch

| Disciplina | Conteúdo | Aplicação no projeto | Status | Etapa | Wiki |
|---|---|---|---|---|---|
| GitHub Actions | Workflows, jobs, matriz | `ci.yml` | Obrigatório | 14 | `disciplinas/github-actions` |
| | Build e publicação de imagens | GHCR com tag por SHA | Obrigatório | 14 | `conceitos/workflow-github-actions` |
| | Segurança do pipeline | Permissões mínimas, SHA, OIDC | Aplicado | 14 | `conceitos/melhores-praticas-github-actions` |
| Azure DevOps | Boards e Pipelines | Substituídos por GitHub Projects e Actions | Alternativa | — | `disciplinas/azure-devops` |
| Azure (cloud) | ACR, AKS, Key Vault | Etapa opcional | Aplicado (opcional) | 15 | `conceitos/aks` |
| AWS | IAM, VPC, ECR, EKS | Não usados | Alternativa | — | `disciplinas/aws` |
| NoSQL | MongoDB | Livro de doações | Aplicado | 6 | `conceitos/mongodb` |
| | Redis | Cache do painel | Aplicado | 5 | `conceitos/redis` |
| | DynamoDB | Não usado | Alternativa | — | `conceitos/dynamodb` |
| Elasticsearch | Índice, analyzer, Query DSL, fuzzy, relevância | Busca de campanhas | Aplicado | 5 | `disciplinas/elasticsearch` |
| TC Fase 4 | Zero credenciais no código | Secrets e Key Vault | Aplicado | 11, 15 | `conceitos/cofre-de-segredos` |

## Fase 5 — Agilidade, Segurança e IA

| Disciplina | Conteúdo | Aplicação no projeto | Status | Etapa | Wiki |
|---|---|---|---|---|---|
| Metodologias ágeis | Scrum | Sprints, papéis, cerimônias | Aplicado | Todas | `conceitos/scrum` |
| | Kanban | GitHub Projects com WIP | Aplicado | Todas | `conceitos/kanban` |
| | XP | TDD, pareamento, CI, refatoração | Aplicado | Todas | `conceitos/extreme-programming` |
| | Lean | Fatiamento vertical, eliminar desperdício (YAGNI) | Aplicado | Todas | `conceitos/pensamento-lean` |
| | Histórias, INVEST, critérios | Backlog e Gherkin | Aplicado | 2 | `conceitos/historias-de-usuario` |
| SOLID | Cinco princípios | [Seção 16](16-padroes-de-codigo.md) | Aplicado | 4–8 | `disciplinas/solid` |
| LGPD | Bases legais, direitos, anonimização, criptografia | [Seção 11](../../specs/product/seguranca-e-lgpd.md), RF08 | Aplicado | 4, 16 | `disciplinas/lgpd` |
| Blazor e Razor | Componentes, render modes, formulários, autenticação | `ConexaoSolidaria.Web` | Aplicado | 8 | `disciplinas/blazor-razor` |
| IA em .NET (introdução) | Semantic Kernel, IA local | Assistente de descrição de campanha | Bônus | 8 | `conceitos/etica-e-explicabilidade-em-ia` |
| Hackathon | Requisitos do enunciado | Todo o projeto | Obrigatório | Todas | `fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria` |
