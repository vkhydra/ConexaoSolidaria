<#
.SYNOPSIS
    Cria dados de demonstração pela API: campanhas, um doador e uma doação, e confere o painel de transparência.

.DESCRIPTION
    Usa só as APIs públicas (Identity.Api e Campanhas.Api), nunca o banco. Pode ser repetido: campanhas e doador que já
    existem são reaproveitados. As credenciais do gestor vêm de deploy/.env (SEED_GESTOR_EMAIL e SEED_GESTOR_SENHA), ou dos
    parâmetros. Os dados do doador são fictícios (CPF de teste válido) e servem só para a demonstração.

.PARAMETER IdentityUrl
    Endereço da Identity.Api. Padrão: http://localhost:8081.

.PARAMETER CampanhasUrl
    Endereço da Campanhas.Api. Padrão: http://localhost:8082.

.PARAMETER EmailGestor
    E-mail do gestor da ONG. Padrão: SEED_GESTOR_EMAIL de deploy/.env.

.PARAMETER SenhaGestor
    Senha do gestor. Padrão: SEED_GESTOR_SENHA de deploy/.env.

.EXAMPLE
    pwsh ./scripts/seed-demo.ps1
#>
[CmdletBinding()]
param(
    [string]$IdentityUrl = 'http://localhost:8081',
    [string]$CampanhasUrl = 'http://localhost:8082',
    [string]$EmailGestor,
    [string]$SenhaGestor
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$arquivoEnv = Join-Path $raiz 'deploy/.env'

function Ler-Env {
    param([string]$Chave)
    if (-not (Test-Path $arquivoEnv)) { return $null }
    $linha = Get-Content $arquivoEnv | Where-Object { $_ -match "^$Chave=" } | Select-Object -First 1
    if ($linha) { return ($linha -replace "^$Chave=", '') }
    return $null
}

if (-not $EmailGestor) { $EmailGestor = Ler-Env 'SEED_GESTOR_EMAIL' }
if (-not $SenhaGestor) { $SenhaGestor = Ler-Env 'SEED_GESTOR_SENHA' }
if (-not $EmailGestor -or -not $SenhaGestor) {
    throw 'Informe -EmailGestor e -SenhaGestor, ou gere o deploy/.env com scripts/gerar-env.ps1.'
}

$emailDoador = 'doador.demo@exemplo.com'
$senhaDoador = 'Doador@2026Demo'
$cpfDoador = '529.982.247-25'

function Invoke-Api {
    # Devolve @{ Status; Corpo } sem lançar exceção em respostas 4xx/5xx.
    param([string]$Metodo, [string]$Url, $Corpo = $null, [string]$Token = $null, [hashtable]$Cabecalhos = @{})
    $parametros = @{ Method = $Metodo; Uri = $Url; SkipHttpErrorCheck = $true; StatusCodeVariable = 'status'; Headers = $Cabecalhos }
    if ($Token) { $parametros.Headers['Authorization'] = "Bearer $Token" }
    if ($null -ne $Corpo) {
        $parametros.Body = ($Corpo | ConvertTo-Json -Depth 5)
        $parametros.ContentType = 'application/json'
    }
    $resposta = Invoke-RestMethod @parametros
    return @{ Status = $status; Corpo = $resposta }
}

function Entrar {
    param([string]$Email, [string]$Senha)
    $r = Invoke-Api 'POST' "$IdentityUrl/api/v1/auth/login" @{ email = $Email; senha = $Senha }
    if ($r.Status -ne 200) { throw "Login falhou (HTTP $($r.Status))." }
    return $r.Corpo.accessToken
}

Write-Host '1/5 Entrando como gestor...'
$tokenGestor = Entrar $EmailGestor $SenhaGestor

Write-Host '2/5 Criando campanhas de demonstração...'
$hoje = (Get-Date).ToString('yyyy-MM-dd')
$fim = (Get-Date).AddDays(60).ToString('yyyy-MM-dd')
$campanhasDemo = @(
    @{ titulo = 'Natal Solidário 2026'; descricao = 'Cestas de Natal para famílias atendidas pela ONG Esperança Solidária.'; meta = 20000.00 },
    @{ titulo = 'Material Escolar para 2027'; descricao = 'Kits de material escolar para crianças em idade de alfabetização.'; meta = 8000.00 },
    @{ titulo = 'Inverno Solidário'; descricao = 'Agasalhos e cobertores para pessoas em situação de rua.'; meta = 5000.00 }
)

$existentes = Invoke-Api 'GET' "$CampanhasUrl/api/v1/campanhas?pageSize=50" $null $tokenGestor
if ($existentes.Status -ne 200) { throw "Não foi possível listar campanhas (HTTP $($existentes.Status))." }
$titulosExistentes = @($existentes.Corpo.items | ForEach-Object { $_.titulo })

$idCampanhaDemo = $null
foreach ($campanha in $campanhasDemo) {
    if ($titulosExistentes -contains $campanha.titulo) {
        Write-Host "   já existe: $($campanha.titulo)"
        $idCampanhaDemo ??= ($existentes.Corpo.items | Where-Object { $_.titulo -eq $campanha.titulo } | Select-Object -First 1).id
        continue
    }
    $r = Invoke-Api 'POST' "$CampanhasUrl/api/v1/campanhas" @{
        titulo = $campanha.titulo; descricao = $campanha.descricao
        dataInicio = $hoje; dataFim = $fim; metaFinanceira = $campanha.meta
    } $tokenGestor
    if ($r.Status -ne 201) { throw "Criação de '$($campanha.titulo)' falhou (HTTP $($r.Status))." }
    Write-Host "   criada: $($campanha.titulo)"
    $idCampanhaDemo ??= $r.Corpo.id
}

Write-Host '3/5 Cadastrando o doador de demonstração...'
$cadastro = Invoke-Api 'POST' "$IdentityUrl/api/v1/doadores" @{
    nomeCompleto = 'Doador Demonstração'; email = $emailDoador; cpf = $cpfDoador
    senha = $senhaDoador; aceitePoliticaPrivacidade = $true
}
switch ($cadastro.Status) {
    201 { Write-Host '   doador cadastrado.' }
    409 { Write-Host '   doador já cadastrado; reaproveitando.' }
    default { throw "Cadastro do doador falhou (HTTP $($cadastro.Status))." }
}

Write-Host '4/5 Fazendo uma doação...'
$tokenDoador = Entrar $emailDoador $senhaDoador
$painelAntes = Invoke-Api 'GET' "$CampanhasUrl/api/v1/transparencia/campanhas"
$totalAntes = [decimal]((@($painelAntes.Corpo) | Where-Object { $_.id -eq $idCampanhaDemo }).totalArrecadado)

$doacao = Invoke-Api 'POST' "$CampanhasUrl/api/v1/doacoes" @{ idCampanha = $idCampanhaDemo; valorDoacao = 50.00 } $tokenDoador @{ 'Idempotency-Key' = [guid]::NewGuid().ToString() }
if ($doacao.Status -ne 202) { throw "Doação falhou (HTTP $($doacao.Status))." }
Write-Host "   doação aceita: $($doacao.Corpo.doacaoId)"

Write-Host '5/5 Aguardando o Worker somar ao painel de transparência...'
$esperado = $totalAntes + 50
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $painel = Invoke-Api 'GET' "$CampanhasUrl/api/v1/transparencia/campanhas"
    $total = [decimal]((@($painel.Corpo) | Where-Object { $_.id -eq $idCampanhaDemo }).totalArrecadado)
    if ($total -ge $esperado) {
        Write-Host "   total arrecadado: R$ $total (era R$ $totalAntes). Ponta a ponta OK."
        exit 0
    }
}
throw "O total não chegou a R$ $esperado em 30 segundos (atual: R$ $total). Veja os logs do arrecadacao-worker."
