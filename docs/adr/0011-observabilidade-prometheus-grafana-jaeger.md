# ADR 0011 — Observabilidade com Prometheus, Grafana e Jaeger

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A Fase 3 cobre Prometheus, Grafana, Zabbix, APMs e tracing. Para operar os serviços, o projeto precisa de `/health`, `/metrics` e dashboard com métricas reais.

## Decisão

Prometheus-net para `/metrics`; OpenTelemetry para traces (exportados via OTLP para o Jaeger); Serilog em JSON no stdout; kube-prometheus-stack (Prometheus, Grafana, Alertmanager, kube-state-metrics, node-exporter). Zabbix opcional.

## Alternativas consideradas

Datadog ou New Relic (SaaS; dependem de conta e custo); OpenTelemetry também para métricas (o exporter Prometheus do OTel .NET ainda é pré-lançamento).

## Consequências

Stack gratuita e local; dashboards e alertas versionados como código.

## Referências

- [Observabilidade](../../specs/product/observabilidade.md)
