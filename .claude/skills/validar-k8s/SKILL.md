---
name: validar-k8s
description: Valida os manifestos Kubernetes do projeto (Kustomize) contra o cluster e contra as regras de segurança, recursos e observabilidade da spec. Use antes de commitar arquivos em deploy/k8s/ ou valores de Helm.
---

# Validar manifestos Kubernetes

Regras em `specs/product/infraestrutura.md` e `specs/product/seguranca-e-lgpd.md`.

## Passos

1. Renderize o overlay:
   ```bash
   kubectl kustomize deploy/k8s/overlays/local > /tmp/render.yaml
   ```
   (No Windows sem `/tmp`, use um arquivo na pasta temporária do sistema.)
2. Valide no servidor (Minikube ligado):
   ```bash
   kubectl apply -k deploy/k8s/overlays/local --dry-run=server
   kubectl diff -k deploy/k8s/overlays/local
   ```
3. Revise cada workload do arquivo renderizado:

   | Item | Regra |
   |---|---|
   | Imagem | Tag `sha-<commit>` ou `X.Y.Z`; nunca `latest` |
   | Recursos | `requests` e `limits` de CPU e memória |
   | Probes | `startupProbe`, `livenessProbe` em `/health/live`, `readinessProbe` em `/health/ready` |
   | Segurança do pod | `runAsNonRoot: true`, `seccompProfile: RuntimeDefault`, `automountServiceAccountToken: false` |
   | Segurança do container | `allowPrivilegeEscalation: false`, `readOnlyRootFilesystem: true`, `capabilities.drop: [ALL]` |
   | Configuração | `envFrom` com ConfigMap e Secret; nenhum valor sensível em ConfigMap |
   | Segredos | Só `*.example.yaml` versionados; nenhum `Secret` com valor real no repositório |
   | Labels | `app.kubernetes.io/name` e `app.kubernetes.io/part-of: conexao-solidaria` |
   | Deploy | `RollingUpdate` com `maxUnavailable: 0`; PDB `minAvailable: 1` nas APIs |
   | Rede | `NetworkPolicy` liberando `infra` só para `conexao` |
   | Observabilidade | `ServiceMonitor` com o label `release` do kube-prometheus-stack |

4. Relate em tabela "Recurso | Regra | Situação | Correção".
5. Corrigido, faça o commit com a skill `commit-gitmoji`: `🧱 build(k8s): <descrição>`.

## Regras

- `kubectl apply` real e `kubectl delete` só com confirmação do usuário.
- Nunca imprimir o conteúdo de Secrets do cluster.
