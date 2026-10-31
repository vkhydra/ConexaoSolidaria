# Infraestrutura

[← README](../../README.md)

Referências: `wiki:disciplinas/docker`, `wiki:disciplinas/kubernetes`, `wiki:disciplinas/orquestracao-k8s`, `wiki:disciplinas/api-gateway`, `wiki:disciplinas/serverless-apps`, `wiki:boas-praticas/docker`, `wiki:boas-praticas/kubernetes`.

## Imagens Docker

Um Dockerfile multi-stage por componente (`src/**/Dockerfile`), contexto de build na raiz do repositório.

```dockerfile
# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
# 1) só o que muda pouco: restore em camada própria
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/BuildingBlocks/ConexaoSolidaria.Contracts/*.csproj src/BuildingBlocks/ConexaoSolidaria.Contracts/
COPY src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/*.csproj src/BuildingBlocks/ConexaoSolidaria.ServiceDefaults/
COPY src/Services/Campanhas/Campanhas.Domain/*.csproj src/Services/Campanhas/Campanhas.Domain/
COPY src/Services/Campanhas/Campanhas.Application/*.csproj src/Services/Campanhas/Campanhas.Application/
COPY src/Services/Campanhas/Campanhas.Infrastructure/*.csproj src/Services/Campanhas/Campanhas.Infrastructure/
COPY src/Services/Campanhas/Campanhas.Api/*.csproj src/Services/Campanhas/Campanhas.Api/
RUN dotnet restore src/Services/Campanhas/Campanhas.Api/Campanhas.Api.csproj --locked-mode
# 2) código
COPY src/ src/
RUN dotnet publish src/Services/Campanhas/Campanhas.Api/Campanhas.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD ["dotnet", "Campanhas.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Campanhas.Api.dll"]
```

| Regra | Motivo |
|---|---|
| Restore antes de copiar o código | Cache de camadas: mudança de código não refaz o restore |
| `--locked-mode` | Build reproduzível com `packages.lock.json` |
| Imagem final sem SDK | Menor superfície de ataque e tamanho |
| `USER app` | Não root (usuário padrão das imagens .NET 8+) |
| `HEALTHCHECK` | A imagem `aspnet` não tem `curl`; o próprio app responde ao argumento `--healthcheck` chamando `/health/live` |
| Sem segredos em `ARG`/`ENV` | Ficariam no histórico da imagem |
| Tag fixa | `sha-<commit>`; nunca `latest` |
| Migration bundle | Estágio extra `migrator` gera `efbundle` para o Job de migration |

`.dockerignore` na raiz: `**/bin`, `**/obj`, `.git`, `.vs`, `.vscode`, `**/TestResults`, `.env*`, `deploy/`, `docs/`, `tests/`.

## docker-compose (desenvolvimento)

`deploy/docker-compose.yml` sobe toda a stack sem Kubernetes (Etapa 10).

| Serviço | Imagem | Porta local | Healthcheck |
|---|---|---|---|
| `postgres` | `postgres:17` | 5432 | `pg_isready` |
| `mongodb` | `mongo:8` | 27017 | `mongosh --eval "db.adminCommand('ping')"` |
| `redis` | `redis:8` | 6379 | `redis-cli ping` |
| `rabbitmq` | `rabbitmq:4-management` | 5672, 15672 | `rabbitmq-diagnostics -q ping` |
| `elasticsearch` | `docker.elastic.co/elasticsearch/elasticsearch:9.x` | 9200 | `curl -fsu ... /_cluster/health` |
| `jaeger` | `jaegertracing/all-in-one` | 16686, 4317 | — |
| `migrator-identity`, `migrator-campanhas` | Imagens próprias (estágio `migrator`) | — | Executa e encerra |
| `identity-api`, `campanhas-api` | Imagens próprias | 8081, 8082 | `/health/ready` |
| `arrecadacao-worker` | Imagem própria | 9090 | `/health/ready` |
| `notificacoes-function` | Imagem própria | — | — |
| `web` | Imagem própria | 8080 | `/health/live` |

- `depends_on` com `condition: service_healthy` (e `service_completed_successfully` para os migrators).
- Credenciais via `env_file: .env`; modelo `deploy/.env.example`.
- Volumes nomeados para os bancos; rede nomeada `conexao`.
- Profile `zabbix` opcional.
- Versões com tag fixa (major.minor) definidas no momento da implementação.

## Kubernetes local

### Cluster
```powershell
minikube start --cpus=6 --memory=12g --addons=metrics-server
minikube image load <imagem>:<tag>     # ou pull do GHCR (repositório público)
minikube tunnel                         # expõe o LoadBalancer do Kong
```

### Organização (Kustomize)
```text
deploy/k8s/
├── base/
│   ├── kustomization.yaml
│   ├── namespaces.yaml
│   ├── infra/        # postgres, mongodb, redis, rabbitmq, elasticsearch (StatefulSets + PVC + Services)
│   ├── apps/         # deployments, services, configmaps, hpa, pdb, jobs de migration, networkpolicies
│   ├── kong/         # ingress, kongplugins
│   ├── keda/         # scaledobjects, triggerauthentications
│   └── secrets/      # *.example.yaml (modelos sem valores)
└── overlays/
    ├── local/        # réplicas e recursos menores, Swagger ligado, imagens locais
    └── azure/        # imagens do ACR, Key Vault (CSI), Ingress público
```

Aplicação: `kubectl apply -k deploy/k8s/overlays/local`. Validação antes: `kubectl apply -k ... --dry-run=server` e `kubectl diff -k ...`.

### Recursos por workload (overlay local)

| Workload | Tipo | Réplicas | Requests (CPU/mem) | Limits (CPU/mem) | Escala |
|---|---|---|---|---|---|
| identity-api | Deployment | 2 | 100m / 128Mi | 500m / 256Mi | HPA 2–4, CPU 70% |
| campanhas-api | Deployment | 2 | 100m / 160Mi | 500m / 320Mi | HPA 2–6, CPU 70% |
| arrecadacao-worker | Deployment | 1 | 100m / 128Mi | 500m / 256Mi | KEDA 1–4, fila > 20 msgs/réplica |
| notificacoes-function | Deployment | 0 | 50m / 96Mi | 250m / 192Mi | KEDA 0–3, fila > 10 |
| web | Deployment | 2 | 100m / 192Mi | 500m / 384Mi | Fixo |
| postgres | StatefulSet | 1 | 250m / 256Mi | 1 / 512Mi | — |
| mongodb | StatefulSet | 1 | 250m / 256Mi | 1 / 512Mi | — |
| rabbitmq | StatefulSet | 1 | 250m / 256Mi | 1 / 512Mi | — |
| elasticsearch | StatefulSet | 1 | 500m / 1Gi | 1 / 1.5Gi | — (heap 512 MB) |
| redis | Deployment | 1 | 50m / 64Mi | 250m / 128Mi | — |

Somando Kong, observabilidade e KEDA, o uso fica em ~9–10 GB, dentro dos 12 GB do Minikube.

### Modelo de Deployment (trecho)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: campanhas-api
  namespace: conexao
  labels: { app.kubernetes.io/name: campanhas-api, app.kubernetes.io/part-of: conexao-solidaria }
spec:
  replicas: 2
  strategy: { type: RollingUpdate, rollingUpdate: { maxUnavailable: 0, maxSurge: 1 } }
  selector: { matchLabels: { app.kubernetes.io/name: campanhas-api } }
  template:
    metadata:
      labels: { app.kubernetes.io/name: campanhas-api, app.kubernetes.io/part-of: conexao-solidaria }
    spec:
      serviceAccountName: campanhas-api
      automountServiceAccountToken: false
      securityContext: { runAsNonRoot: true, seccompProfile: { type: RuntimeDefault } }
      topologySpreadConstraints:
        - { maxSkew: 1, topologyKey: kubernetes.io/hostname, whenUnsatisfiable: ScheduleAnyway,
            labelSelector: { matchLabels: { app.kubernetes.io/name: campanhas-api } } }
      containers:
        - name: campanhas-api
          image: ghcr.io/<owner>/conexao-solidaria/campanhas-api:sha-<commit>
          ports: [{ name: http, containerPort: 8080 }]
          envFrom:
            - configMapRef: { name: campanhas-api-config }
            - secretRef: { name: campanhas-api-secrets }
          resources:
            requests: { cpu: 100m, memory: 160Mi }
            limits: { cpu: 500m, memory: 320Mi }
          startupProbe:   { httpGet: { path: /health/live, port: http }, failureThreshold: 30, periodSeconds: 2 }
          livenessProbe:  { httpGet: { path: /health/live, port: http }, periodSeconds: 10 }
          readinessProbe: { httpGet: { path: /health/ready, port: http }, periodSeconds: 5 }
          securityContext:
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities: { drop: [ALL] }
          volumeMounts: [{ name: tmp, mountPath: /tmp }]
      volumes: [{ name: tmp, emptyDir: {} }]
```

### Demais objetos

| Objeto | Uso |
|---|---|
| `Service` (ClusterIP) | Um por aplicação e banco; comunicação pelo nome (`http://campanhas-api.conexao.svc:80`) |
| `ConfigMap` | Configuração não sensível por aplicação; `deploy/rabbitmq/definitions.json`; dashboard Grafana |
| `Secret` | Credenciais; criado por `scripts/k8s-secrets.ps1` (nunca versionado) |
| `PersistentVolumeClaim` | Via `volumeClaimTemplates` dos StatefulSets (StorageClass padrão do Minikube) |
| `HorizontalPodAutoscaler` | identity-api e campanhas-api (`behavior.scaleDown.stabilizationWindowSeconds: 120`) |
| `PodDisruptionBudget` | APIs e web com `minAvailable: 1` |
| `Job` | `migrate-identity`, `migrate-campanhas` (migration bundle), executados antes do rollout |
| `NetworkPolicy` | Negar tudo por padrão em `infra`; liberar só de `conexao` nas portas dos bancos |
| `ServiceMonitor` | Coleta de `/metrics` (label `release` igual ao nome do release Helm do kube-prometheus-stack) |
| `PrometheusRule` | Alertas de [observabilidade](observabilidade.md#alertas-deployobservabilityalertsprometheus-rulesyaml) |

### Ordem de subida (`scripts/up.ps1`)
1. `minikube start` e `minikube tunnel` (outro terminal).
2. Helm: KEDA, kube-prometheus-stack, Jaeger, Kong Ingress Controller.
3. `scripts/k8s-secrets.ps1`.
4. `kubectl apply -k deploy/k8s/overlays/local`.
5. `kubectl wait --for=condition=complete job/migrate-identity job/migrate-campanhas -n conexao --timeout=180s`.
6. `kubectl rollout status deploy -n conexao`.
7. `scripts/seed-demo.ps1`.
8. `kubectl get pods -A` com tudo `Running`/`Completed`.

## API Gateway (Kong)

| Item | Definição |
|---|---|
| Instalação | `helm install kong kong/ingress -n kong --create-namespace` (Kong Ingress Controller, DB-less) |
| Exposição | Service `LoadBalancer` do proxy via `minikube tunnel`; host `api.conexao.local` e `app.conexao.local` no arquivo `hosts` |
| TLS | Certificado mkcert em Secret `tls-conexao`, referenciado nos `Ingress` |
| Admin API | Só ClusterIP; nunca exposta |

Rotas (`Ingress` com `ingressClassName: kong`, sem `strip-path`):

| Caminho | Serviço | Plugins extras |
|---|---|---|
| `/api/v1/auth` | identity-api | `rate-limiting` 10/min por IP |
| `/api/v1/doadores` | identity-api | `rate-limiting` 30/min |
| `/api/v1/campanhas`, `/api/v1/transparencia`, `/api/v1/doacoes`, `/api/v1/admin` | campanhas-api | `rate-limiting` 60/min |
| `/` em `app.conexao.local` | web | afinidade de sessão (cookie) para o circuito do Blazor |

Plugins globais (`KongClusterPlugin`): `correlation-id` (`X-Correlation-ID`, ecoa na resposta), `prometheus`, `request-size-limiting` (1 MB), `cors` (origem `https://app.conexao.local`), `response-transformer` com cabeçalhos de segurança.

## Serverless com KEDA

| Item | Definição |
|---|---|
| Instalação | `helm install keda kedacore/keda -n keda --create-namespace` |
| Função | Imagem `mcr.microsoft.com/azure-functions/dotnet-isolated` com a `Notificacoes.Function` |
| Gatilho | `[RabbitMQTrigger("notificacoes-recibo", ConnectionStringSetting = "RabbitMqConnection")]` |
| `ScaledObject` | `minReplicaCount: 0`, `maxReplicaCount: 3`, trigger `rabbitmq` (`mode: QueueLength`, `value: "10"`), `TriggerAuthentication` lendo o host do Secret |
| Worker | `ScaledObject` com `minReplicaCount: 1`, `maxReplicaCount: 4`, fila `doacao-recebida` |
| Demonstração | Fila vazia → 0 pods; doação → pod da função sobe, processa e volta a 0 após `cooldownPeriod` (60 s) |

## Cloud opcional (Azure)

Só se o grupo decidir demonstrar na nuvem (Etapa 15). Tudo apagado após a gravação.

| Recurso | Uso | Observação |
|---|---|---|
| Grupo de recursos `rg-conexao-solidaria` | Tudo junto, apagado junto | Tags `projeto`, `ambiente`, `owner` |
| Alerta de orçamento | Primeiro recurso a criar | Ex.: US$ 20 |
| Azure Container Registry (Basic) | Imagens | Admin user desabilitado; pull pelo AKS (`--attach-acr`) |
| AKS (1 node pool, 2 nós B-series) | Cluster gerenciado | Workload Identity, Container Insights opcional |
| Key Vault | Segredos | CSI Secrets Store Driver no AKS |
| Bancos | Mesmos StatefulSets do local | Evita custo de serviços gerenciados |

IaC: `deploy/azure/main.bicep` + `az deployment group create`. CD: `.github/workflows/cd.yml` com OIDC ([CI/CD](ci-cd.md)). Remoção: `az group delete -n rg-conexao-solidaria --yes`.
