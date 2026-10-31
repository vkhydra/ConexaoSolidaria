# Manual de implementação

[← README](../README.md)

Passo a passo para colocar o Conexão Solidária no ar a partir de um clone do repositório, em Windows, macOS ou Linux. Público: integrantes do grupo e avaliadores. O código é construído conforme o [plano de execução](plano-de-execucao.md); este manual descreve como **implantar e operar** o resultado.

Todos os comandos rodam em **PowerShell 7 (`pwsh`)**, igual nos três sistemas. Quando um passo muda por sistema, a diferença aparece numa tabela.

## Modos de implantação

| Modo | Para quê | Tempo aproximado | Seção |
|---|---|---|---|
| A — docker-compose | Desenvolvimento e demonstração rápida | 10 min | 22.5 |
| B — Kubernetes local (Minikube) | Ambiente completo das fases 2 a 4: Kong, HPA, KEDA, Prometheus, Grafana | 30 min | 22.6 |
| C — Azure (opcional) | Demonstração na nuvem | 45 min | 22.10 |

## Antes de começar

- [ ] Pré-requisitos instalados e verificados ([Pré-requisitos](pre-requisitos.md)).
- [ ] Docker em execução (`docker version` mostra `Server`).
- [ ] Pelo menos 14 GB de memória livre para o modo B.
- [ ] Portas livres no modo A: 5432, 6379, 5672, 15672, 9200, 27017, 16686, 8080, 8081, 8082, 9090.

## Obter o código

```powershell
gh repo clone <owner>/conexao-solidaria
cd conexao-solidaria
dotnet tool restore                          # dotnet-ef, husky, reportgenerator
pwsh ./scripts/verificar-pre-requisitos.ps1  # deve terminar sem pendências
```

O primeiro `dotnet restore` (ou `build`) instala os hooks de git do Husky.Net automaticamente.

## Configurar segredos e variáveis

1. Copiar o modelo:
   ```powershell
   Copy-Item deploy/.env.example deploy/.env
   ```
2. Preencher `deploy/.env` (o arquivo é ignorado pelo git; nunca commitar):

   | Variável | Conteúdo | Como gerar |
   |---|---|---|
   | `POSTGRES_PASSWORD`, `MONGO_PASSWORD`, `RABBITMQ_PASSWORD`, `REDIS_PASSWORD`, `ELASTIC_PASSWORD` | Senhas dos serviços de infraestrutura | Comando 1 abaixo |
   | `Cpf__ChaveCifra`, `Cpf__ChaveHash`, `Autenticacao__ChaveHashEmail` | Chaves de 256 bits (Base64) para cifra e hash do CPF e hash do e-mail nas tentativas de login | Comando 1 abaixo (uma execução para cada) |
   | `Jwt__ChavePrivadaPem`, `Jwt__ChavePublicaPem` | Par de chaves RSA 2048 do JWT | Comando 2 abaixo |
   | `Seed__GestorEmail`, `Seed__GestorSenha` | Primeiro usuário `GestorONG` | Escolher; a senha segue a política do RF03 |
   | `RabbitMqConnection` | URI do RabbitMQ usada pelo gatilho da função (`amqp://usuario:senha@rabbitmq:5672`) | Montar com o usuário e a senha do RabbitMQ |
   | `Lgpd__VersaoPolitica` | Versão da política de privacidade exibida no cadastro | Valor atual: `2026-10` |

   Comando 1 — valor aleatório de 256 bits:
   ```powershell
   [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
   ```
   Comando 2 — par de chaves RSA em PEM:
   ```powershell
   $rsa = [System.Security.Cryptography.RSA]::Create(2048)
   $rsa.ExportPkcs8PrivateKeyPem() | Set-Content deploy/jwt-privada.pem   # ignorado pelo git (*.pem)
   $rsa.ExportSubjectPublicKeyInfoPem() | Set-Content deploy/jwt-publica.pem
   ```
   No `.env`, as chaves PEM vão numa linha só, com `\n` no lugar das quebras de linha.
3. Conferir que nada sensível ficou fora do `.gitignore`: `git status` não deve listar `deploy/.env` nem `*.pem`.

## Depuração no VS Code (opcional)

Para depurar os serviços fora de containers, com os bancos no Docker:

1. Copiar `deploy/.env` para `deploy/.env.dev` (ignorado pelo git) e trocar os nomes dos hosts pelos endereços locais (`postgres` → `localhost`, `rabbitmq` → `localhost`, e assim por diante).
2. No VS Code, rodar a tarefa **infra local (compose)** (`Ctrl+Shift+P` → *Tasks: Run Task*).
3. Na aba *Run and Debug*, escolher **Backend completo** ou **Sistema completo** (`.vscode/launch.json`).

| Serviço | Endereço na depuração |
|---|---|
| Identity.Api | http://localhost:8081/swagger |
| Campanhas.Api | http://localhost:8082/swagger |
| Arrecadacao.Worker | http://localhost:9090/metrics |
| Web | http://localhost:8080 |

## Modo A — docker-compose

1. Compilar e testar (opcional, mas recomendado):
   ```powershell
   dotnet build -c Release
   dotnet test -c Release
   ```
2. Subir a stack:
   ```powershell
   docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
   ```
3. Aguardar tudo saudável (os migrators terminam com `Exited (0)`):
   ```powershell
   docker compose -f deploy/docker-compose.yml ps
   ```
4. Carregar os dados de demonstração:
   ```powershell
   pwsh ./scripts/seed-demo.ps1 -IdentityUrl http://localhost:8081 -CampanhasUrl http://localhost:8082
   ```
5. Acessar:

   | Serviço | Endereço |
   |---|---|
   | Aplicação web | http://localhost:8080 |
   | Identity.Api (Swagger) | http://localhost:8081/swagger |
   | Campanhas.Api (Swagger) | http://localhost:8082/swagger |
   | Métricas do Worker | http://localhost:9090/metrics |
   | RabbitMQ (painel) | http://localhost:15672 (usuário e senha do `.env`) |
   | Jaeger | http://localhost:16686 |

No modo A não há Kong, Prometheus nem Grafana; para eles, usar o modo B.

## Modo B — Kubernetes local (Minikube)

O script `scripts/up.ps1` executa os passos 1 a 9 em sequência. Abaixo, os mesmos passos manuais, para entender ou depurar.

### Passo 1 — Cluster
```powershell
minikube start --cpus=6 --memory=12g --addons=metrics-server
kubectl get nodes      # STATUS Ready
```

### Passo 2 — Túnel (deixar aberto em outro terminal)
```powershell
minikube tunnel
```

| Sistema | Observação |
|---|---|
| Windows | Terminal como administrador |
| macOS e Linux | Pede a senha de `sudo` |

### Passo 3 — Componentes de plataforma (Helm)
```powershell
helm upgrade --install keda kedacore/keda -n keda --create-namespace --wait
helm upgrade --install kube-prometheus-stack prometheus-community/kube-prometheus-stack `
  -n observability --create-namespace -f deploy/helm/kube-prometheus-stack-values.yaml --wait
helm upgrade --install jaeger jaegertracing/jaeger -n observability -f deploy/helm/jaeger-values.yaml --wait
helm upgrade --install kong kong/ingress -n kong --create-namespace -f deploy/helm/kong-values.yaml --wait
```
Em `zsh` e `bash`, trocar a crase (`` ` ``) do fim da linha por `\`, ou escrever o comando numa linha só.

### Passo 4 — Certificado HTTPS
```powershell
mkcert -cert-file deploy/tls.crt -key-file deploy/tls.key api.conexao.local app.conexao.local
kubectl create namespace conexao --dry-run=client -o yaml | kubectl apply -f -
kubectl create secret tls tls-conexao --cert deploy/tls.crt --key deploy/tls.key -n conexao
```
Os arquivos `deploy/tls.*` são ignorados pelo git.

### Passo 5 — Secrets
```powershell
pwsh ./scripts/k8s-secrets.ps1 -EnvFile deploy/.env
```
Cria os `Secret` de cada serviço a partir do `.env` (modelos sem valores em `deploy/k8s/base/secrets/*.example.yaml`).

### Passo 6 — Imagens

| Opção | Comando | Quando usar |
|---|---|---|
| Imagens do GHCR (padrão) | Nenhum: o overlay `local` aponta para `ghcr.io/<owner>/conexao-solidaria/<servico>:sha-<commit>` | Avaliação e vídeo |
| Build local | `minikube image build -t conexao/<servico>:dev -f <caminho>/Dockerfile .` para cada serviço e `kustomize edit set image` no overlay `local` | Testar mudança ainda não publicada |

### Passo 7 — Aplicação
```powershell
kubectl apply -k deploy/k8s/overlays/local --dry-run=server   # valida
kubectl apply -k deploy/k8s/overlays/local
kubectl wait --for=condition=complete job/migrate-identity job/migrate-campanhas -n conexao --timeout=180s
kubectl rollout status deploy -n conexao --timeout=5m
```

### Passo 8 — Nomes no arquivo `hosts`

| Sistema | Arquivo | IP |
|---|---|---|
| Windows | `C:\Windows\System32\drivers\etc\hosts` (editor como administrador) | `127.0.0.1` |
| macOS | `/etc/hosts` (`sudo`) | `127.0.0.1` |
| Linux | `/etc/hosts` (`sudo`) | `EXTERNAL-IP` de `kubectl get svc -n kong` |

Linha a acrescentar:
```text
<IP>  api.conexao.local  app.conexao.local
```

### Passo 9 — Dados de demonstração
```powershell
pwsh ./scripts/seed-demo.ps1 -IdentityUrl https://api.conexao.local -CampanhasUrl https://api.conexao.local
```

### Endereços do modo B

| Serviço | Endereço | Acesso |
|---|---|---|
| Aplicação web | https://app.conexao.local | Pelo Kong |
| API | https://api.conexao.local/api/v1 | Pelo Kong |
| Swagger | https://api.conexao.local/swagger | Só no overlay `local` |
| Grafana | http://localhost:3000 | `kubectl port-forward -n observability svc/kube-prometheus-stack-grafana 3000:80` |
| Prometheus | http://localhost:9091 | `kubectl port-forward -n observability svc/kube-prometheus-stack-prometheus 9091:9090` |
| Jaeger | http://localhost:16686 | `kubectl port-forward -n observability svc/jaeger-query 16686:16686` |
| RabbitMQ | http://localhost:15672 | `kubectl port-forward -n infra svc/rabbitmq 15672:15672` |

Senha do `admin` do Grafana:
```powershell
$b64 = kubectl get secret -n observability kube-prometheus-stack-grafana -o jsonpath="{.data.admin-password}"
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($b64))
```

Os arquivos `deploy/helm/*-values.yaml` são criados nas Etapas 11 a 13 do plano.

## Verificação da implantação (smoke test)

Executar em ordem; cada linha depende da anterior.

```powershell
$api = "https://api.conexao.local/api/v1"

# 1. Painel público responde
Invoke-RestMethod "$api/transparencia/campanhas"

# 2. Login do gestor
$login = Invoke-RestMethod "$api/auth/login" -Method Post -ContentType application/json `
  -Body (@{ email = "<Seed__GestorEmail>"; senha = "<Seed__GestorSenha>" } | ConvertTo-Json)
$gestor = @{ Authorization = "Bearer $($login.accessToken)" }

# 3. Criar campanha
$hoje = Get-Date -Format yyyy-MM-dd
$fim  = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
$campanha = Invoke-RestMethod "$api/campanhas" -Method Post -Headers $gestor -ContentType application/json `
  -Body (@{ titulo = "Teste de implantação"; descricao = "Campanha de verificação"; dataInicio = $hoje; dataFim = $fim; metaFinanceira = 1000 } | ConvertTo-Json)

# 4. Cadastrar doador e entrar (CPF fictício válido)
Invoke-RestMethod "$api/doadores" -Method Post -ContentType application/json `
  -Body (@{ nomeCompleto = "Doador Teste"; email = "doador.teste@exemplo.com"; cpf = "529.982.247-25"; senha = "Senha@123"; aceitePoliticaPrivacidade = $true } | ConvertTo-Json)
$loginDoador = Invoke-RestMethod "$api/auth/login" -Method Post -ContentType application/json `
  -Body (@{ email = "doador.teste@exemplo.com"; senha = "Senha@123" } | ConvertTo-Json)
$doador = @{ Authorization = "Bearer $($loginDoador.accessToken)" }

# 5. Doar (202) e conferir o total depois de alguns segundos
Invoke-RestMethod "$api/doacoes" -Method Post -Headers $doador -ContentType application/json `
  -Body (@{ idCampanha = $campanha.id; valorDoacao = 150 } | ConvertTo-Json)
Start-Sleep -Seconds 5
(Invoke-RestMethod "$api/transparencia/campanhas") | Where-Object id -eq $campanha.id
```

| Verificação | Resultado esperado |
|---|---|
| Pods | `kubectl get pods -A`: todos `Running` ou `Completed` |
| Painel | Passo 5 mostra `totalArrecadado` 150.00 |
| Recibo | `kubectl logs -n conexao -l app.kubernetes.io/name=notificacoes-function` contém `ReciboEmitido` sem nome, e-mail ou CPF |
| KEDA | `kubectl get deploy notificacoes-function -n conexao` volta a 0 réplicas cerca de 60 s depois |
| Métricas | Painel "Conexão Solidária" no Grafana com doações e requisições |
| Trace | No Jaeger, serviço `campanhas-api`, operação `POST /api/v1/doacoes`, com spans até o Worker |
| Rate limit | 11 logins em menos de 1 minuto retornam 429 no último |

Se o CPF de teste já estiver cadastrado (409), usar outro CPF fictício válido.

## Testes automatizados e de carga

```powershell
dotnet test -c Release --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;JsonSummary"
pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90

k6 run -e BASE_URL=https://api.conexao.local tests/e2e/load/painel.js
k6 run -e BASE_URL=https://api.conexao.local tests/e2e/load/doacao.js
k6 run -e BASE_URL=https://api.conexao.local tests/e2e/load/busca.js
```
Os testes de integração precisam do Docker ligado (Testcontainers). Detalhes em [Testes](testes.md).

## Atualizar uma versão (Rolling Update)

```powershell
cd deploy/k8s/overlays/local
kustomize edit set image ghcr.io/<owner>/conexao-solidaria/campanhas-api=ghcr.io/<owner>/conexao-solidaria/campanhas-api:sha-<novo>
cd ../../../..
kubectl apply -k deploy/k8s/overlays/local
kubectl rollout status deploy/campanhas-api -n conexao
```
Se `kustomize` não estiver instalado, usar `kubectl kustomize` para conferir o resultado e editar o campo `images` do `kustomization.yaml` à mão.

Desfazer: `kubectl rollout undo deploy/campanhas-api -n conexao`. Migrations novas rodam antes, pelos Jobs `migrate-*` (apagar o Job antigo antes de aplicar de novo: `kubectl delete job migrate-campanhas -n conexao`).

## Modo C — Azure (opcional)

Só se o grupo decidir demonstrar na nuvem ([infraestrutura](../specs/product/infraestrutura.md#cloud-opcional-azure)). Custo real: **apagar tudo** logo depois da gravação.

```powershell
az login
az group create -n rg-conexao-solidaria -l brazilsouth
# criar o alerta de orçamento no portal antes de seguir
az deployment group create -g rg-conexao-solidaria -f deploy/azure/main.bicep
az aks get-credentials -g rg-conexao-solidaria -n <nome-do-aks>
```
Depois, repetir os passos 3 a 9 do modo B usando o overlay `azure` (segredos vêm do Key Vault). O deploy contínuo fica no `cd.yml` ([CI/CD](../specs/product/ci-cd.md#cd-opcional-aks)).

Remoção: `az group delete -n rg-conexao-solidaria --yes --no-wait`.

## Desligar e limpar

| Objetivo | Comando |
|---|---|
| Parar o modo A mantendo os dados | `docker compose -f deploy/docker-compose.yml stop` |
| Apagar o modo A com os dados | `docker compose -f deploy/docker-compose.yml down -v` |
| Pausar o cluster | `minikube stop` |
| Apagar o cluster | `minikube delete` |
| Remover os nomes locais | Apagar a linha `api.conexao.local app.conexao.local` do `hosts` |
| Remover a nuvem | `az group delete -n rg-conexao-solidaria --yes` |

## Solução de problemas

| Sintoma | Causa provável | Como resolver |
|---|---|---|
| Pods em `Pending` | Memória insuficiente no Minikube | `kubectl describe pod <pod>`; conferir 12 GB no Minikube e a memória do Docker ([pré-requisitos](pre-requisitos.md)) |
| Elasticsearch em `CrashLoopBackOff` | `vm.max_map_count` baixo (Linux) | Passo 5 da [instalação no Linux](pre-requisitos.md#instalação-no-linux-ubuntu-2404) e `kubectl delete pod -n infra -l app.kubernetes.io/name=elasticsearch` |
| `ImagePullBackOff` | Tag inexistente no GHCR ou nome errado | `kubectl describe pod`; conferir a tag `sha-*` no GHCR ou usar build local (passo 6) |
| `https://app.conexao.local` não abre | Túnel parado ou `hosts` sem a linha | Passo 2 aberto em outro terminal; passo 8 |
| Aviso de certificado no navegador | Autoridade do mkcert não instalada | `mkcert -install` e reabrir o navegador |
| Job `migrate-*` falhou | Banco ainda subindo ou senha errada | `kubectl logs job/migrate-campanhas -n conexao`; corrigir o `.env`, rodar o passo 5 e reaplicar |
| 401 em todas as rotas protegidas | Chave pública do JWT diferente da privada | Gerar o par de novo ([configurar segredos](#configurar-segredos-e-variáveis)) e repetir o passo 5 |
| 429 no login | Bloqueio por tentativas (RF01) ou rate limit do Kong | Esperar 15 min ou usar outro e-mail |
| Total do painel não muda | Worker parado ou mensagem na fila de erro | `kubectl logs -n conexao deploy/arrecadacao-worker`; fila `*_error` no painel do RabbitMQ |
| Função não escala de 0 para 1 | `ScaledObject` sem acesso ao RabbitMQ | `kubectl describe scaledobject -n conexao`; conferir o `TriggerAuthentication` |
| Busca retorna 503 | Elasticsearch fora do ar | Esperar o pod ficar `Ready`; reindexar com `POST /admin/reindexar` |
| `permission denied` no Docker (Linux) | Usuário fora do grupo `docker` | `sudo usermod -aG docker $USER` e entrar de novo na sessão |
| Porta ocupada no modo A | Outro serviço local usando a porta | Parar o serviço ou mudar a porta no `docker-compose.yml` |
| Script `.ps1` bloqueado (Windows) | Política de execução | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
