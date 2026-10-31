# Runbook — Latência do painel alta

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `LatenciaPainelAlta` |
| Condição | p95 de `GET /transparencia/campanhas` acima de 300 ms por 10 minutos |
| Severidade | Aviso |

## Sintoma

O painel de transparência demora para carregar. O SLO de latência (p95 < 300 ms) está em risco.

## Impacto

Experiência ruim para visitantes; consumo do orçamento de erro.

## Diagnóstico

1. Ver a métrica de cache no Grafana: `sum by (resultado) (rate(painel_cache_total[5m]))`. Muitas faltas ou erros indicam problema no Redis.
2. Ver o Redis:
   ```bash
   kubectl get pods -n infra -l app.kubernetes.io/name=redis
   kubectl exec -n infra deploy/redis -- redis-cli -a "$REDIS_PASSWORD" info stats
   ```
3. Ver a consulta no banco (trace no Jaeger: span `ObterPainelTransparencia` → span do Npgsql).
4. Ver CPU das réplicas da `campanhas-api` e se o HPA está no máximo: `kubectl get hpa -n conexao`.

## Resolução

| Causa | Ação |
|---|---|
| Redis indisponível | Reiniciar o pod do Redis; o painel volta a usar o cache sozinho |
| Consulta lenta | Conferir o índice `ix_campanhas_status_data_fim` (`EXPLAIN ANALYZE` da consulta do painel) |
| CPU saturada | Aumentar `maxReplicas` do HPA ou o limite de CPU no overlay |

## Prevenção

Teste de carga `tests/e2e/load/painel.js` antes de cada demonstração.
