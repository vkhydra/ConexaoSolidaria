# 02 — Pré-requisitos

[← README](../README.md) · Anterior: [01 — Visão geral](../specs/product/overview.md) · Próximo: [03 — Processo ágil](processo-agil.md)

Tudo o que precisa estar pronto antes da primeira linha de código, em **Windows, macOS ou Linux**. Corresponde à **Etapa 0** do [plano de execução](spec/20-plano-de-execucao.md).

## 2.1 Sistemas operacionais suportados

| Sistema | Versão mínima | Gerenciador de pacotes | Docker | Terminal recomendado |
|---|---|---|---|---|
| Windows | Windows 11 (ou 10 22H2) 64 bits | `winget` | Docker Desktop com backend WSL 2 | PowerShell 7 (`pwsh`) |
| macOS | macOS 14 Sonoma, Intel ou Apple Silicon | Homebrew (`brew`) | Docker Desktop | Terminal ou iTerm2 com `zsh` |
| Linux | Ubuntu 24.04 LTS (referência; outras distros adaptam os comandos) | `apt` e `snap` | Docker Engine (sem Desktop) | `bash` |

Os scripts do projeto (`scripts/*.ps1` e hooks de git) são escritos em **PowerShell 7**, que roda nos três sistemas. Assim existe um script só para cada tarefa.

## 2.2 Hardware

| Recurso | Mínimo | Recomendado | Motivo |
|---|---|---|---|
| RAM | 16 GB | 32 GB | O Minikube recebe 12 GB (bancos, Elasticsearch, Prometheus e serviços) |
| CPU | 4 núcleos | 8 núcleos | O Minikube recebe 6 vCPUs |
| Disco livre | 40 GB | 60 GB | Imagens Docker, volumes e o disco do Minikube |
| Virtualização | Habilitada na BIOS/UEFI | — | Exigida pelo WSL 2 (Windows) e pelo Docker Desktop (Windows e macOS) |
| Arquitetura | x64 ou ARM64 | — | Todas as imagens usadas têm versão `amd64` e `arm64` (Apple Silicon funciona sem emulação) |

## 2.3 Software necessário

Mesmas ferramentas e versões em todos os sistemas. Os comandos de instalação de cada sistema estão nas seções 2.4 a 2.6.

| # | Ferramenta | Versão | Uso no projeto | Verificação |
|---|---|---|---|---|
| 1 | Git | 2.4x+ | Versionamento | `git --version` |
| 2 | .NET SDK | 10 (LTS) | Build, testes, EF Core | `dotnet --list-sdks` |
| 3 | Docker | Atual | Containers, Testcontainers, driver do Minikube | `docker version` |
| 4 | Minikube | Atual | Kubernetes local | `minikube version` |
| 5 | kubectl | Compatível com o Minikube (±1 minor) | Aplicar manifestos | `kubectl version --client` |
| 6 | Helm | 3.x | Kong, KEDA, Prometheus, Jaeger | `helm version` |
| 7 | k6 | Atual | Testes de carga | `k6 version` |
| 8 | Azure Functions Core Tools | v4 | Rodar a função localmente | `func --version` |
| 9 | GitHub CLI | Atual | Repositório, PRs, Actions | `gh --version` |
| 10 | PowerShell 7 | 7.x | Hooks de git e scripts | `pwsh --version` |
| 11 | Node.js | LTS | MCP do Playwright e Core Tools no Linux | `node --version` |
| 12 | mkcert | Atual | HTTPS local no Kong | `mkcert -help` |
| 13 | Postman | Atual | Coleção da API | — |
| 14 | OBS Studio | Atual | Gravação do vídeo | — |
| 15 | IDE | VS Code + C# Dev Kit **ou** Visual Studio 2026 (só Windows) **ou** JetBrains Rider | Desenvolvimento | `code --version` |
| 16 | Claude Code | Atual | Assistente com skills e MCP | `claude --version` |
| 17 | Azure CLI | Atual | Só para a cloud opcional | `az version` |

## 2.4 Instalação no Windows

Num terminal PowerShell **como administrador**. Depois de instalar, abrir um terminal novo para atualizar o `PATH`.

```powershell
wsl --install                       # reiniciar o computador ao final
winget install Git.Git
winget install Microsoft.DotNet.SDK.10
winget install Docker.DockerDesktop
winget install Kubernetes.minikube
winget install Kubernetes.kubectl
winget install Helm.Helm
winget install k6 --source winget
winget install Microsoft.Azure.FunctionsCoreTools
winget install GitHub.cli
winget install Microsoft.PowerShell
winget install OpenJS.NodeJS.LTS
winget install FiloSottile.mkcert
winget install Postman.Postman
winget install OBSProject.OBSStudio
winget install Microsoft.VisualStudioCode
winget install Microsoft.AzureCLI   # opcional (cloud)
```

**Configuração do Docker Desktop:**
1. Settings → General: **Use the WSL 2 based engine** ligado.
2. Memória da VM do WSL suficiente para os 12 GB do Minikube. No arquivo `%UserProfile%\.wslconfig`:
   ```ini
   [wsl2]
   memory=14GB
   processors=6
   ```
3. Reiniciar o WSL: `wsl --shutdown` e abrir o Docker Desktop de novo.

**Política de execução de scripts** (uma vez, para rodar os `.ps1` do projeto):
```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

## 2.5 Instalação no macOS

1. Ferramentas de linha de comando da Apple: `xcode-select --install`.
2. Homebrew: seguir o comando de instalação de [brew.sh](https://brew.sh) e, no Apple Silicon, adicionar `/opt/homebrew/bin` ao `PATH` como o instalador indica.
3. Ferramentas:
   ```bash
   brew install git minikube kubectl helm k6 gh mkcert nss node@24 azure-cli
   brew tap azure/functions && brew install azure-functions-core-tools@4
   brew install --cask docker powershell postman obs visual-studio-code
   ```
4. .NET SDK 10: instalador `.pkg` (Arm64 ou x64) de [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0). Conferir com `dotnet --list-sdks`.
5. Docker Desktop → Settings → Resources: **memória 14 GB**, **6 CPUs**, disco 60 GB. Aplicar e reiniciar.

O `azure-cli` é opcional (só para a cloud).

## 2.6 Instalação no Linux (Ubuntu 24.04)

1. Pacotes base e .NET SDK 10:
   ```bash
   sudo apt update && sudo apt install -y git curl ca-certificates software-properties-common
   sudo add-apt-repository -y ppa:dotnet/backports
   sudo apt install -y dotnet-sdk-10.0
   ```
2. Docker Engine e permissão para o usuário (encerrar a sessão e entrar de novo depois):
   ```bash
   curl -fsSL https://get.docker.com | sh
   sudo usermod -aG docker $USER
   ```
3. Kubernetes e demais ferramentas:
   ```bash
   curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
   sudo install minikube-linux-amd64 /usr/local/bin/minikube && rm minikube-linux-amd64
   sudo snap install kubectl --classic
   sudo snap install helm --classic
   sudo snap install k6
   sudo snap install powershell --classic
   sudo snap install code --classic
   sudo snap install postman
   sudo apt install -y gh mkcert libnss3-tools obs-studio
   ```
   Em ARM64, trocar `minikube-linux-amd64` por `minikube-linux-arm64`.
4. Node.js LTS com o nvm (instalação em [github.com/nvm-sh/nvm](https://github.com/nvm-sh/nvm)) e Core Tools:
   ```bash
   nvm install --lts
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```
5. Requisito do Elasticsearch: no Linux o Minikube com driver Docker usa o kernel da própria máquina, então o limite de mapeamento de memória precisa ser ajustado:
   ```bash
   echo 'vm.max_map_count=262144' | sudo tee /etc/sysctl.d/99-elasticsearch.conf
   sudo sysctl --system
   ```
6. Azure CLI (opcional): `curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash`.

No Linux não há VM do Docker: os 12 GB do Minikube saem direto da memória da máquina.

## 2.7 Configuração comum (todos os sistemas)

Os comandos abaixo são iguais em PowerShell, `zsh` e `bash`.

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

### Minikube
```bash
minikube config set driver docker
minikube config set cpus 6
minikube config set memory 12g
minikube start --addons=metrics-server
kubectl get nodes   # o nó deve estar Ready
minikube stop       # parar até a Etapa 11
```

### Repositórios Helm (usados nas Etapas 11 a 13)
```bash
helm repo add kong https://charts.konghq.com
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add kedacore https://kedacore.github.io/charts
helm repo add jaegertracing https://jaegertracing.github.io/helm-charts
helm repo update
```

### Certificado local (mkcert)
```bash
mkcert -install    # instala a autoridade certificadora local no sistema e nos navegadores
```

### Ferramentas .NET locais
Ficam no manifesto `.config/dotnet-tools.json` do repositório (criado na Etapa 3), para todos usarem as mesmas versões:

| Ferramenta | Uso |
|---|---|
| `dotnet-ef` | Migrations e migration bundles |
| `husky` | Hooks de git (build, formato, mensagem de commit) |
| `dotnet-reportgenerator-globaltool` | Relatório e verificação de cobertura |

Restauração: `dotnet tool restore`.

### Git
```bash
git config --global user.name "Seu Nome"
git config --global user.email "seu-email-do-github"
git config --global init.defaultBranch main
gh auth login
```

Fim de linha: o `.gitattributes` do repositório (Etapa 3) define LF para todo o código. Para não brigar com ele:

| Sistema | Comando |
|---|---|
| Windows | `git config --global core.autocrlf true` |
| macOS e Linux | `git config --global core.autocrlf input` |

## 2.8 Diferenças entre os sistemas

| Item | Windows | macOS | Linux |
|---|---|---|---|
| Arquivo `hosts` (domínios `api.conexao.local` e `app.conexao.local`) | `C:\Windows\System32\drivers\etc\hosts` (editor como administrador) | `/etc/hosts` (`sudo`) | `/etc/hosts` (`sudo`) |
| IP no `hosts` | `127.0.0.1` | `127.0.0.1` | `EXTERNAL-IP` do Service do Kong (`kubectl get svc -n kong`) |
| `minikube tunnel` | Terminal como administrador | Pede a senha (`sudo`) | Pede a senha (`sudo`) |
| Memória do Docker | `.wslconfig` | Docker Desktop → Resources | Sem limite (usa a máquina) |
| `vm.max_map_count` (Elasticsearch) | Já ajustado na VM do Docker Desktop | Já ajustado na VM do Docker Desktop | Ajuste manual (seção 2.6, passo 5) |
| Rodar scripts do projeto | `pwsh ./scripts/<nome>.ps1` | `pwsh ./scripts/<nome>.ps1` | `pwsh ./scripts/<nome>.ps1` |
| Separador de caminho nos exemplos | `\` funciona, mas a spec usa `/` (aceito pelo PowerShell) | `/` | `/` |

## 2.9 Contas

| Conta | Uso | Observação |
|---|---|---|
| GitHub | Repositório público, Actions, GHCR, Projects | Todos os integrantes como colaboradores |
| Discord | Comunicação do grupo | Usernames entram no relatório |
| Miro | Event Storming e Domain Storytelling | Plano gratuito basta |
| YouTube ou Google Drive | Publicação do vídeo | Link não listado ou acesso por link |
| Azure (opcional) | Etapa de cloud | Azure for Students; criar **alerta de orçamento** antes de qualquer recurso |

## 2.10 Conhecimento prévio (LLMWiki)

Leitura mínima antes de começar, na LLMWiki (somente leitura; no Windows em `C:\Victor\Fiap\LLMWiki\wiki`):

| Tema | Páginas |
|---|---|
| Enunciado | `fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria`, `disciplinas/tech-challenge` |
| Visão das fases | `fases/fase-1-*` a `fases/fase-5-*` |
| Boas práticas | `boas-praticas/*` (uma página por disciplina) |
| Conceitos centrais | `conceitos/microsservicos`, `conceitos/rabbitmq`, `conceitos/jwt`, `conceitos/kubernetes`, `conceitos/grafana`, `conceitos/consistencia-eventual` |

## 2.11 Verificação final

Script `scripts/verificar-pre-requisitos.ps1` (criado na Etapa 3) detecta o sistema (`$IsWindows`, `$IsMacOS`, `$IsLinux`), executa todas as verificações da seção 2.3 e mostra o que falta com o comando de instalação do sistema detectado. Até lá, rodar manualmente em `pwsh`:

```powershell
git --version; dotnet --list-sdks; docker version --format '{{.Server.Version}}'
minikube version --short; kubectl version --client; helm version --short
k6 version; func --version; gh auth status; pwsh --version; node --version
```

**Pronto quando:** todos os comandos respondem sem erro, o `minikube start` deixa o nó `Ready` e todos os integrantes têm acesso ao repositório.
