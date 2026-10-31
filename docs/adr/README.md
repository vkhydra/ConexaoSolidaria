# Architecture Decision Records (ADRs)

[← README](../../README.md)

Registro das decisões de arquitetura do Conexão Solidária. Cada arquivo segue o formato **Status, Contexto, Decisão, Alternativas consideradas, Consequências e Referências**.

## Índice

| ADR | Decisão | Status |
|---|---|---|
| [0001](0001-escolha-da-stack.md) | Escolha da stack | Aceito |
| [0002](0002-monorepo.md) | Monorepo | Aceito |
| [0003](0003-bounded-contexts-e-componentes.md) | Três bounded contexts e quatro componentes de backend | Aceito |
| [0004](0004-worker-unico-escritor-do-total.md) | Worker como único escritor do total arrecadado | Aceito |
| [0005](0005-doacao-assincrona-com-outbox.md) | Doação assíncrona com Transactional Outbox | Aceito |
| [0006](0006-consumidores-idempotentes.md) | Consumidores idempotentes | Aceito |
| [0007](0007-persistencia-poliglota.md) | Persistência poliglota | Aceito |
| [0008](0008-jwt-rs256-emitido-pela-identity.md) | JWT RS256 emitido só pela Identity | Aceito |
| [0009](0009-kong-como-api-gateway.md) | Kong como API Gateway | Aceito |
| [0010](0010-notificacoes-serverless-com-keda.md) | Notificações como função serverless com KEDA | Aceito |
| [0011](0011-observabilidade-prometheus-grafana-jaeger.md) | Observabilidade com Prometheus, Grafana e Jaeger | Aceito |
| [0012](0012-rabbitmq-com-masstransit-8.md) | RabbitMQ com MassTransit 8.x | Aceito |
| [0013](0013-bibliotecas-sem-licenca-comercial.md) | Bibliotecas com licença comercial evitadas | Aceito |
| [0014](0014-github-actions-e-ghcr.md) | GitHub Actions e GHCR | Aceito |
| [0015](0015-execucao-local-com-cloud-opcional.md) | Execução local com cloud opcional na Azure | Aceito |
| [0016](0016-frontend-blazor-web-app.md) | Frontend Blazor Web App | Aceito |
| [0017](0017-spec-driven-development.md) | Spec-Driven Development com Claude Code | Aceito |
| [0018](0018-escopo-trabalho-substitutivo.md) | Escopo: trabalho substitutivo da Fase 3 | Aceito |

## Regras

1. Toda decisão arquitetural nova vira um ADR antes do código ([constituição](../../specs/constitution.md), princípio de rastreabilidade).
2. Numeração sequencial de 4 dígitos; nome do arquivo em `kebab-case` sem acentos.
3. ADR aceito não é editado. Uma mudança gera um ADR novo, e o antigo passa a ter o status `Substituído por NNNN`.
4. Status possíveis: `Proposto`, `Aceito`, `Rejeitado`, `Substituído por NNNN`, `Descontinuado`.
5. Criação assistida pela skill `nova-adr` (`.claude/skills/nova-adr/SKILL.md`).

## Modelo

```markdown
# ADR NNNN — <título da decisão>

- **Status:** Proposto
- **Data:** AAAA-MM-DD
- **Decisores:** <nomes ou "equipe do projeto">

## Contexto
<o problema, as forças envolvidas e as restrições>

## Decisão
<o que foi decidido, no presente: "Usar ...">

## Alternativas consideradas
- **<alternativa>:** <por que não foi escolhida>

## Consequências
<efeitos positivos, negativos e o que muda no projeto>

## Referências
- <links para specs, features e páginas da LLMWiki>
```
