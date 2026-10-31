# Runbook — Serviço sem réplicas

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Alerta | `ServicoSemReplicas` |
| Condição | `kube_deployment_status_replicas_available == 0` por 1 minuto (exceto `notificacoes-function`, que escala a zero) |
| Severidade | Crítica |

## Sintoma

Um Deployment do namespace `conexao` (ou um componente de `infra`) está sem nenhuma réplica disponível.

## Impacto

Funcionalidade totalmente fora do ar.

## Diagnóstico

```bash
kubectl get deploy,statefulset -n conexao
kubectl get deploy,statefulset -n infra
kubectl get events -n conexao --sort-by=.lastTimestamp | tail -20
kubectl describe deploy <nome> -n conexao
```

| Evento | Causa provável |
|---|---|
| `FailedScheduling` (Insufficient memory/cpu) | Cluster sem recursos |
| `ImagePullBackOff` | Tag inexistente no GHCR |
| `CrashLoopBackOff` | Ver [pod reiniciando](pod-reiniciando.md) |
| Readiness falhando | Dependência fora do ar (`/health/ready`) |

## Resolução

| Causa | Ação |
|---|---|
| Falta de recursos | `kubectl top nodes`; reduzir réplicas de componentes opcionais (Jaeger, Zabbix) ou aumentar a memória do Minikube |
| Imagem errada | Corrigir a tag no overlay (`kustomize edit set image`) e reaplicar |
| Dependência fora | Resolver primeiro o componente de `infra` |
| Versão com defeito | `kubectl rollout undo deploy/<nome> -n conexao` |

## Prevenção

PDB com `minAvailable: 1`, `RollingUpdate` com `maxUnavailable: 0` e readiness probe em todos os serviços ([infraestrutura](../../specs/product/infraestrutura.md)).
