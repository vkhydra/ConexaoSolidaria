# ADR 0001 — Escolha da stack

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O Hackathon da Fase 5 pede um MVP com foco em escalabilidade, observabilidade e automação, avaliado pela arquitetura. O projeto também precisa aplicar o conteúdo das 5 fases do curso ([cobertura das fases](../cobertura-das-fases.md)). O enunciado da Fase 2 pede .NET 8 ou superior, e o .NET 8 sai de suporte em novembro de 2026.

A stack precisa:

- rodar inteira numa máquina de 16 GB com Windows, macOS ou Linux;
- ter licença livre (repositório público, sem custo);
- ser a ensinada no curso, sempre que houver opção.

## Decisão

Usar .NET 10 (LTS) em todos os projetos, com o SDK fixado em `global.json`, e a stack abaixo. Cada linha com ADR próprio detalha a escolha.

| Camada | Tecnologia | ADR |
|---|---|---|
| Linguagem e runtime | C# / .NET 10 LTS | este |
| Organização do código | Monorepo com `ConexaoSolidaria.slnx` | [0002](0002-monorepo.md) |
| Serviços | ASP.NET Core Minimal API, Worker Service | [0003](0003-bounded-contexts-e-componentes.md) |
| Mensageria | RabbitMQ 4 + MassTransit 8.x com Transactional Outbox | [0005](0005-doacao-assincrona-com-outbox.md), [0012](0012-rabbitmq-com-masstransit-8.md) |
| Dados | PostgreSQL 17, MongoDB 8, Redis 8, Elasticsearch 9 | [0007](0007-persistencia-poliglota.md) |
| Segurança | JWT RS256, BCrypt, AES-GCM | [0008](0008-jwt-rs256-emitido-pela-identity.md) |
| Serverless | Azure Functions isolated worker + KEDA | [0010](0010-notificacoes-serverless-com-keda.md) |
| Gateway | Kong Ingress Controller (DB-less) | [0009](0009-kong-como-api-gateway.md) |
| Observabilidade | prometheus-net, OpenTelemetry, Jaeger, Serilog, kube-prometheus-stack | [0011](0011-observabilidade-prometheus-grafana-jaeger.md) |
| Containers e orquestração | Docker, Kubernetes (Minikube) com Kustomize | [0015](0015-execucao-local-com-cloud-opcional.md) |
| CI/CD | GitHub Actions + GHCR | [0014](0014-github-actions-e-ghcr.md) |
| Frontend | Blazor Web App | [0016](0016-frontend-blazor-web-app.md) |
| Testes | xUnit v3, Shouldly, NSubstitute, Testcontainers, Reqnroll, bUnit, k6 | [0013](0013-bibliotecas-sem-licenca-comercial.md) |
| Processo de desenvolvimento | Spec-Driven Development com Claude Code | [0017](0017-spec-driven-development.md) |

## Alternativas consideradas

- **.NET 8 LTS:** sai de suporte durante o projeto.
- **.NET 9 STS:** já está fora de suporte.
- **Java/Spring ou Node.js:** fora do conteúdo do curso de Arquitetura .NET.

## Consequências

- O projeto tem acesso a `HybridCache`, `.slnx`, melhorias de Minimal API, OpenAPI nativo e comentários XML no OpenAPI.
- Exceção possível só na função serverless, se o runtime do Azure Functions ainda não suportar .NET 10 ([0010](0010-notificacoes-serverless-com-keda.md)).
- Toda versão de pacote fica centralizada em `Directory.Packages.props` com lock files, para builds reproduzíveis.

## Referências

- [Visão geral do produto](../../specs/product/overview.md)
- [Arquitetura](../../specs/product/arquitetura.md)
- `wiki:fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria`
