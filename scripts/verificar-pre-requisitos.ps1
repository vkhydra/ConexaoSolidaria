<#
.SYNOPSIS
    Confere as ferramentas necessárias para o projeto e mostra como instalar o que falta.

.DESCRIPTION
    Detecta o sistema operacional (Windows, macOS ou Linux), testa cada ferramenta de docs/pre-requisitos.md
    e mostra o comando de instalação do sistema detectado para as que faltam.

    As ferramentas têm uma "fase de uso" do roadmap (specs/roadmap.md): as essenciais para desenvolver e
    testar (Git, .NET, PowerShell, Docker) são obrigatórias; as de Kubernetes, carga e nuvem só são
    cobradas com -Completo, porque só servem a partir das fases 9 a 15.

    Termina com código 1 se faltar alguma ferramenta cobrada.

.PARAMETER Completo
    Cobra também as ferramentas das fases de infraestrutura (Minikube, kubectl, Helm, k6, Functions Core Tools,
    mkcert, GitHub CLI, Node.js e Azure CLI).

.EXAMPLE
    pwsh ./scripts/verificar-pre-requisitos.ps1

.EXAMPLE
    pwsh ./scripts/verificar-pre-requisitos.ps1 -Completo
#>
[CmdletBinding()]
param(
    [switch]$Completo
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sistema = if ($IsWindows) { 'Windows' } elseif ($IsMacOS) { 'macOS' } elseif ($IsLinux) { 'Linux' } else { 'Desconhecido' }

function Testar-Comando {
    # Executa o comando de verificação e devolve a primeira linha da saída, ou $null se falhar.
    param([string]$Comando, [string[]]$Argumentos)
    if (-not (Get-Command $Comando -ErrorAction SilentlyContinue)) { return $null }
    try {
        # Captura a saída inteira antes de pegar a primeira linha: interromper o pipeline cedo
        # deixaria $LASTEXITCODE sem valor, e o modo estrito trata isso como erro.
        $saida = & $Comando @Argumentos 2>&1
        if ($LASTEXITCODE -ne 0) { return $null }
        return "$($saida | Select-Object -First 1)".Trim()
    } catch {
        return $null
    }
}

# Nome | comando | argumentos | fase de uso | obrigatória | instalação (windows | macos | linux)
$ferramentas = @(
    @{ Nome = 'Git'; Cmd = 'git'; Args = @('--version'); Fase = 'Todas'; Obrigatoria = $true
       Instalar = @{ Windows = 'winget install Git.Git'; macOS = 'brew install git'; Linux = 'sudo apt install -y git' } },
    @{ Nome = '.NET SDK 10'; Cmd = 'dotnet'; Args = @('--version'); Fase = 'Todas'; Obrigatoria = $true; MinimoMajor = 10
       Instalar = @{ Windows = 'winget install Microsoft.DotNet.SDK.10'; macOS = 'baixe o .pkg em https://dotnet.microsoft.com/download/dotnet/10.0'; Linux = 'sudo apt install -y dotnet-sdk-10.0' } },
    @{ Nome = 'PowerShell 7'; Cmd = 'pwsh'; Args = @('--version'); Fase = 'Todas (scripts e hooks)'; Obrigatoria = $true
       Instalar = @{ Windows = 'winget install Microsoft.PowerShell'; macOS = 'brew install --cask powershell'; Linux = 'sudo snap install powershell --classic' } },
    @{ Nome = 'Docker (cliente)'; Cmd = 'docker'; Args = @('--version'); Fase = '3 em diante (Testcontainers) e 9'; Obrigatoria = $true
       Instalar = @{ Windows = 'winget install Docker.DockerDesktop'; macOS = 'brew install --cask docker'; Linux = 'curl -fsSL https://get.docker.com | sh' } },
    @{ Nome = 'Docker (serviço ligado)'; Cmd = 'docker'; Args = @('info', '--format', '{{.ServerVersion}}'); Fase = '3 em diante (Testcontainers) e 9'; Obrigatoria = $true
       Instalar = @{ Windows = 'abra o Docker Desktop e espere o ícone ficar verde'; macOS = 'abra o Docker Desktop'; Linux = 'sudo systemctl start docker' } },
    @{ Nome = 'Minikube'; Cmd = 'minikube'; Args = @('version', '--short'); Fase = '10 a 13'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install Kubernetes.minikube'; macOS = 'brew install minikube'; Linux = 'veja docs/pre-requisitos.md (instalação no Linux)' } },
    @{ Nome = 'kubectl'; Cmd = 'kubectl'; Args = @('version', '--client'); Fase = '10 a 13'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install Kubernetes.kubectl'; macOS = 'brew install kubectl'; Linux = 'sudo snap install kubectl --classic' } },
    @{ Nome = 'Helm'; Cmd = 'helm'; Args = @('version', '--short'); Fase = '10 a 13'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install Helm.Helm'; macOS = 'brew install helm'; Linux = 'sudo snap install helm --classic' } },
    @{ Nome = 'k6'; Cmd = 'k6'; Args = @('version'); Fase = '12'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install k6 --source winget'; macOS = 'brew install k6'; Linux = 'sudo snap install k6' } },
    @{ Nome = 'Azure Functions Core Tools'; Cmd = 'func'; Args = @('--version'); Fase = '6 (função de recibo)'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install Microsoft.Azure.FunctionsCoreTools'; macOS = 'brew tap azure/functions; brew install azure-functions-core-tools@4'; Linux = 'npm install -g azure-functions-core-tools@4 --unsafe-perm true' } },
    @{ Nome = 'GitHub CLI'; Cmd = 'gh'; Args = @('--version'); Fase = '1 e 14 (proteção da main, releases)'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install GitHub.cli'; macOS = 'brew install gh'; Linux = 'sudo apt install -y gh' } },
    @{ Nome = 'mkcert'; Cmd = 'mkcert'; Args = @('-help'); Fase = '11 (HTTPS no Kong)'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install FiloSottile.mkcert'; macOS = 'brew install mkcert nss'; Linux = 'sudo apt install -y mkcert libnss3-tools' } },
    @{ Nome = 'Node.js'; Cmd = 'node'; Args = @('--version'); Fase = '8 (MCP do Playwright)'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install OpenJS.NodeJS.LTS'; macOS = 'brew install node'; Linux = 'use o nvm: nvm install --lts' } },
    @{ Nome = 'Azure CLI'; Cmd = 'az'; Args = @('version', '--output', 'tsv', '--query', '"azure-cli"'); Fase = '15 (opcional)'; Obrigatoria = $false
       Instalar = @{ Windows = 'winget install Microsoft.AzureCLI'; macOS = 'brew install azure-cli'; Linux = 'curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash' } }
)

Write-Host "Sistema detectado: $sistema" -ForegroundColor Cyan
Write-Host ''

$resultados = foreach ($f in $ferramentas) {
    $versao = Testar-Comando $f.Cmd $f.Args
    $ok = $null -ne $versao
    $observacao = ''

    if ($ok -and $f.ContainsKey('MinimoMajor')) {
        $major = [int](($versao -split '\.')[0])
        if ($major -lt $f.MinimoMajor) {
            $ok = $false
            $observacao = "versão $versao; é preciso $($f.MinimoMajor) ou mais"
        }
    }

    $cobrada = $f.Obrigatoria -or $Completo
    [pscustomobject]@{
        Ferramenta = $f.Nome
        Situacao   = if ($ok) { 'OK' } elseif ($cobrada) { 'FALTA' } else { 'opcional agora' }
        Versao     = if ($ok) { $versao } else { $observacao }
        Fase       = $f.Fase
        Instalar   = $f.Instalar[$sistema]
        Cobrada    = $cobrada
        Ok         = $ok
    }
}

$resultados | Format-Table Ferramenta, Situacao, Versao, Fase -AutoSize

$faltando = @($resultados | Where-Object { $_.Cobrada -and -not $_.Ok })
$opcionais = @($resultados | Where-Object { -not $_.Cobrada -and -not $_.Ok })

if ($faltando.Count -gt 0) {
    Write-Host 'Faltam ferramentas obrigatórias:' -ForegroundColor Red
    foreach ($f in $faltando) { Write-Host (" - {0}: {1}" -f $f.Ferramenta, $f.Instalar) -ForegroundColor Red }
}

if ($opcionais.Count -gt 0 -and -not $Completo) {
    Write-Host ''
    Write-Host 'Ainda não instaladas, mas só necessárias nas fases indicadas (use -Completo para cobrá-las):' -ForegroundColor Yellow
    foreach ($f in $opcionais) { Write-Host (" - {0} (fase {1}): {2}" -f $f.Ferramenta, $f.Fase, $f.Instalar) -ForegroundColor Yellow }
}

if ($faltando.Count -gt 0) { exit 1 }

Write-Host ''
Write-Host 'Pré-requisitos obrigatórios atendidos.' -ForegroundColor Green
exit 0
