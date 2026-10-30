# 02 — Pré-requisitos

[← Índice](../SPEC.md) · Anterior: [01 — Visão geral](01-visao-geral.md) · Próximo: [03 — Processo ágil](03-processo-agil.md)

Tudo o que precisa estar pronto antes da primeira linha de código. Corresponde à **Etapa 0** do [plano de execução](20-plano-de-execucao.md).

## 2.1 Hardware

| Recurso | Mínimo | Recomendado | Motivo |
|---|---|---|---|
| RAM | 16 GB | 32 GB | O Minikube recebe 12 GB (bancos, Elasticsearch, Prometheus e serviços) |
| CPU | 4 núcleos | 8 núcleos | O Minikube recebe 6 vCPUs |
| Disco livre | 40 GB | 60 GB | Imagens Docker, volumes e o disco do Minikube |
| Virtualização | Habilitada na BIOS | — | Exigida pelo WSL 2 e pelo Docker Desktop |

## 2.2 Software

Instalar com `winget` num terminal PowerShell. Após instalar, abrir um terminal novo para atualizar o `PATH`.

| # | Ferramenta | Versão | Instalação | Verificação |
|---|---|---|---|---|
| 1 | WSL 2 | Atual | `wsl --install` | `wsl --status` |
| 2 | Git | 2.4x+ | `winget install Git.Git` | `git --version` |
| 3 | .NET SDK | 10 (LTS) | `winget install Microsoft.DotNet.SDK.10` | `dotnet --list-sdks` |
| 4 | Docker Desktop | Atual, backend WSL 2 | `winget install Docker.DockerDesktop` | `docker version` |
| 5 | Minikube | Atual | `winget install Kubernetes.minikube` | `minikube version` |
| 6 | kubectl | Compatível com o Minikube (±1 minor) | `winget install Kubernetes.kubectl` | `kubectl version --client` |
| 7 | Helm | 3.x | `winget install Helm.Helm` | `helm version` |
| 8 | k6 | Atual | `winget install k6 --source winget` | `k6 version` |
| 9 | Azure Functions Core Tools | v4 | `winget install Microsoft.Azure.FunctionsCoreTools` | `func --version` |
| 10 | GitHub CLI | Atual | `winget install GitHub.cli` | `gh --version` |
| 11 | Postman | Atual | `winget install Postman.Postman` | — |
| 12 | OBS Studio | Atual | `winget install OBSProject.OBSStudio` | — |
| 13 | IDE | VS Code + C# Dev Kit **ou** Visual Studio 2026 | `winget install Microsoft.VisualStudioCode` | `code --version` |
| 14 | Claude Code | Atual | Extensão do VS Code ou CLI | `claude --version` |
| 15 | Azure CLI (só cloud opcional) | Atual | `winget install Microsoft.AzureCLI` | `az version` |
| 16 | mkcert (HTTPS local) | Atual | `winget install FiloSottile.mkcert` | `mkcert -help` |

### Extensões do VS Code

| Extensão | Uso |
|---|---|
| C# Dev Kit | Build, testes e depuração .NET |
| Docker | Dockerfiles e containers |
| Kubernetes | Manifestos e cluster |
| YAML | Validação de manifestos e workflows |
| EditorConfig | Aplica o `.editorconfig` |
| markdownlint | Padrão dos arquivos de documentação |
| Markdown Preview Mermaid Support | Visualizar os diagramas Mermaid |
| GitHub Actions | Validar workflows |
| Claude Code | Assistente com skills e MCP |

## 2.3 Configuração do ambiente

### Docker Desktop
1. Settings → General: **Use the WSL 2 based engine** ligado.
2. Settings → Resources (WSL): memória da VM do WSL suficiente para os 12 GB do Minikube. No arquivo `%UserProfile%\.wslconfig`:
   ```ini
   [wsl2]
   memory=14GB
   processors=6
   ```
3. Reiniciar o WSL: `wsl --shutdown`.

### Minikube
```powershell
minikube config set driver docker
minikube config set cpus 6
minikube config set memory 12g
minikube start --addons=metrics-server
kubectl get nodes   # o nó deve estar Ready
minikube stop       # parar até a Etapa 11
```

### Repositórios Helm (usados nas Etapas 11 a 13)
```powershell
helm repo add kong https://charts.konghq.com
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jaegertracing https://jaegertracing.github.io/helm-charts
helm repo update
```

### Ferramentas .NET locais
Ficam no manifesto `.config/dotnet-tools.json` do repositório (criado na Etapa 3), para todos usarem as mesmas versões:

| Ferramenta | Uso |
|---|---|
| `dotnet-ef` | Migrations e migration bundles |
| `husky` | Hooks de git (build, formato, mensagem de commit) |
| `dotnet-reportgenerator-globaltool` | Relatório de cobertura |

Restauração: `dotnet tool restore`.

### Git
```powershell
git config --global user.name "Seu Nome"
git config --global user.email "seu-email-do-github"
git config --global init.defaultBranch main
git config --global core.autocrlf true
gh auth login
```

## 2.4 Contas

| Conta | Uso | Observação |
|---|---|---|
| GitHub | Repositório público, Actions, GHCR, Projects | Todos os integrantes como colaboradores |
| Discord | Comunicação do grupo | Usernames entram no relatório |
| Miro | Event Storming e Domain Storytelling | Plano gratuito basta |
| YouTube ou Google Drive | Publicação do vídeo | Link não listado ou acesso por link |
| Azure (opcional) | Etapa de cloud | Azure for Students; criar **alerta de orçamento** antes de qualquer recurso |

## 2.5 Conhecimento prévio (LLMWiki)

Leitura mínima antes de começar, em `C:\Victor\Fiap\LLMWiki\wiki` (somente leitura):

| Tema | Páginas |
|---|---|
| Enunciado | `fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria`, `disciplinas/tech-challenge` |
| Visão das fases | `fases/fase-1-*` a `fases/fase-5-*` |
| Boas práticas | `boas-praticas/*` (uma página por disciplina) |
| Conceitos centrais | `conceitos/microsservicos`, `conceitos/rabbitmq`, `conceitos/jwt`, `conceitos/kubernetes`, `conceitos/grafana`, `conceitos/consistencia-eventual` |

## 2.6 Verificação final

Script `scripts/verificar-pre-requisitos.ps1` (criado na Etapa 3) executa todas as verificações da seção 2.2 e mostra o que falta. Até lá, rodar manualmente:

```powershell
git --version; dotnet --list-sdks; docker version --format '{{.Server.Version}}'
minikube version --short; kubectl version --client; helm version --short
k6 version; func --version; gh auth status
```

**Pronto quando:** todos os comandos respondem sem erro, o `minikube start` deixa o nó `Ready` e todos os integrantes têm acesso ao repositório.
