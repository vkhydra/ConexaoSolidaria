# ADR 0011 — Observabilidade com Prometheus, Grafana e Jaeger

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O enunciado pede `/health`, `/metrics` e dashboard Grafana com métricas reais. A Fase 3 cobre Prometheus, Grafana, Zabbix, APMs e tracing.

## Decisão

Prometheus-net para `/metrics`; OpenTelemetry para traces (exportados via OTLP para o Jaeger); Serilog em JSON no stdout; kube-prometheus-stack (Prometheus, Grafana, Alertmanager, kube-state-metrics, node-exporter). Zabbix opcional.

## Alternativas consideradas

Datadog ou New Relic (SaaS; dependem de conta e custo); OpenTelemetry também para métricas (o exporter Prometheus do OTel .NET ainda é pré-lançamento).

## Consequências

Stack gratuita e local; dashboards e alertas versionados como código.

## Referências

- [Observabilidade](../../specs/product/observabilidade.md)
