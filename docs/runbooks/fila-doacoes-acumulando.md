# Runbook — Fila de doações acumulando

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `FilaDoacoesAcumulando` |
| Condição | Mais de 100 mensagens prontas em `doacao-recebida` por 5 minutos |
| Severidade | Aviso |

## Sintoma

O total do painel demora mais de 5 s para refletir as doações. No Grafana, "Mensagens prontas por fila" sobe sem parar.

## Impacto

Consistência eventual mais lenta (RNF03 em risco). Nenhuma doação é perdida.

## Diagnóstico

1. Ver réplicas e escala do Worker:
   ```bash
   kubectl get deploy arrecadacao-worker -n conexao
   kubectl get scaledobject -n conexao
   kubectl describe scaledobject arrecadacao-worker -n conexao
   ```
2. Ver se o Worker está consumindo (logs e métrica `doacoes_processadas_total`).
3. Ver a saúde do PostgreSQL e do MongoDB (`/health/ready` do Worker na porta 9090).

## Resolução

| Causa | Ação |
|---|---|
| KEDA no máximo (4 réplicas) | Aumentar `maxReplicaCount` no overlay, se houver recursos no cluster |
| KEDA sem acesso ao RabbitMQ | Conferir o `TriggerAuthentication` e o Secret do host |
| Worker travado em dependência | Resolver a dependência; ver [serviço sem réplicas](servico-sem-replicas.md) |
| Pico de demonstração (k6) | Esperar; a fila esvazia sozinha |

## Prevenção

Teste de carga `tests/e2e/load/doacao.js` observando a escala do Worker no Grafana.
