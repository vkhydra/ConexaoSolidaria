# Runbook — Pod reiniciando

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `PodReiniciando` |
| Condição | Mais de 3 reinícios de um container em 15 minutos |
| Severidade | Aviso |

## Sintoma

Pod em `CrashLoopBackOff` ou com o contador `RESTARTS` subindo.

## Impacto

Capacidade reduzida; com 2 réplicas e PDB, o serviço continua no ar, mas sem folga.

## Diagnóstico

```bash
kubectl get pods -n conexao
kubectl describe pod <pod> -n conexao          # Last State, Reason, Events
kubectl logs <pod> -n conexao --previous       # logs da execução que caiu
```

| `Reason` | Significado |
|---|---|
| `OOMKilled` | Memória acima do limite |
| `Error` + exceção no log | Falha na inicialização (configuração, segredo, dependência) |
| `Liveness probe failed` | Processo travado ou probe agressiva demais |

## Resolução

| Causa | Ação |
|---|---|
| `OOMKilled` | Aumentar `limits.memory` no overlay e investigar vazamento (métricas `dotnet_*`) |
| Configuração ausente | Conferir ConfigMap e Secret (`kubectl get secret <nome> -n conexao`) e rodar `scripts/k8s-secrets.ps1` |
| Probe falhando na subida | Aumentar `startupProbe.failureThreshold` |

## Prevenção

Limites de recursos revisados após cada teste de carga.
