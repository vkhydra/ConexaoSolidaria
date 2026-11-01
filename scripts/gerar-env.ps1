<#
.SYNOPSIS
    Gera deploy/.env a partir de deploy/.env.example, com segredos aleatórios para o ambiente local.

.DESCRIPTION
    Preenche senhas, chaves de 32 bytes (Base64) e o par RSA do JWT (PEM em uma linha, com \n). Não sobrescreve um
    .env existente, a menos que -Forcar seja usado. O .env fica fora do Git.

.PARAMETER Forcar
    Substitui o deploy/.env se ele já existir.

.EXAMPLE
    pwsh ./scripts/gerar-env.ps1
#>
[CmdletBinding()]
param(
    [switch]$Forcar
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$modelo = Join-Path $raiz 'deploy/.env.example'
$destino = Join-Path $raiz 'deploy/.env'

if ((Test-Path $destino) -and -not $Forcar) {
    Write-Host "deploy/.env já existe; use -Forcar para substituir."
    exit 0
}

function Novo-Segredo {
    param([int]$Bytes = 24)
    $buffer = [byte[]]::new($Bytes)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
    # Só letras e dígitos: a senha entra em strings de conexão e URLs sem precisar de escape.
    return ([Convert]::ToBase64String($buffer) -replace '[^A-Za-z0-9]', 'x')
}

function Nova-Chave32 {
    $buffer = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
    return [Convert]::ToBase64String($buffer)
}

$rsa = [System.Security.Cryptography.RSA]::Create(2048)
$privada = $rsa.ExportRSAPrivateKeyPem() -replace "`r?`n", '\n'
$publica = $rsa.ExportRSAPublicKeyPem() -replace "`r?`n", '\n'

$valores = @{
    POSTGRES_SENHA                = Novo-Segredo
    MONGO_SENHA                   = Novo-Segredo
    RABBITMQ_SENHA                = Novo-Segredo
    AUTENTICACAO_CHAVE_HASH_EMAIL = Nova-Chave32
    CPF_CHAVE_CIFRA               = Nova-Chave32
    CPF_CHAVE_HASH                = Nova-Chave32
    JWT_CHAVE_PRIVADA_PEM         = "`"$privada`""
    JWT_CHAVE_PUBLICA_PEM         = "`"$publica`""
    SEED_GESTOR_SENHA             = "Gestor@$(Novo-Segredo 9)1"
}

$linhas = foreach ($linha in Get-Content $modelo) {
    if ($linha -match '^([A-Z0-9_]+)=(.*)$' -and $valores.ContainsKey($Matches[1])) {
        "$($Matches[1])=$($valores[$Matches[1]])"
    }
    else {
        $linha
    }
}

Set-Content -Path $destino -Value $linhas -Encoding utf8NoBOM
Write-Host "deploy/.env criado. Senha do gestor: veja SEED_GESTOR_SENHA no arquivo (não é exibida aqui)."
