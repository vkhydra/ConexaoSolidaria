<#
.SYNOPSIS
    Confere se a cobertura de testes atinge o mínimo (linhas e ramos), no total e em cada projeto de src/.

.DESCRIPTION
    Lê o resumo gerado pelo ReportGenerator (Summary.json) e termina com erro (código 1) se o total ou
    algum projeto ficar abaixo do mínimo. Com -ExecutarTestes, antes limpa os resultados antigos, roda os
    testes com cobertura (coverlet) e gera o relatório.

    Projetos sem linhas para medir (ainda vazios) são ignorados. Se nenhum projeto tem linhas para medir
    (início da Etapa 3), a verificação passa.

.PARAMETER Minimo
    Percentual mínimo de linhas e de ramos. Padrão: 90.

.PARAMETER ExecutarTestes
    Roda os testes com cobertura e gera o relatório antes de conferir.

.PARAMETER Filtro
    Filtro de testes do dotnet test, usado só com -ExecutarTestes (ex.: Category=Unit). Vazio roda todos.

.PARAMETER Relatorio
    Caminho do Summary.json do ReportGenerator. Padrão: coverage/Summary.json.

.EXAMPLE
    pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90 -ExecutarTestes

.EXAMPLE
    pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90 -Relatorio coverage/all/Summary.json
#>
[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double]$Minimo = 90,

    [switch]$ExecutarTestes,

    [string]$Filtro = '',

    [string]$Relatorio = 'coverage/Summary.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

function Obter-Valor {
    # Lê uma propriedade que pode não existir no JSON (o ReportGenerator omite o que não se aplica).
    param($Objeto, [string]$Nome)
    $propriedade = $Objeto.PSObject.Properties[$Nome]
    if ($null -eq $propriedade) { return $null }
    return $propriedade.Value
}

if ($ExecutarTestes) {
    foreach ($pasta in 'TestResults', 'coverage') {
        if (Test-Path $pasta) { Remove-Item $pasta -Recurse -Force }
    }

    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar as ferramentas dotnet.' }

    $argumentos = @(
        'test', 'ConexaoSolidaria.slnx', '-c', 'Release',
        '--settings', 'coverlet.runsettings',
        '--collect', 'XPlat Code Coverage',
        '--logger', 'trx',
        '--results-directory', 'TestResults'
    )
    if ($Filtro) { $argumentos += @('--filter', $Filtro) }

    Write-Host "Rodando os testes com cobertura (filtro: '$Filtro')..." -ForegroundColor Cyan
    & dotnet @argumentos
    if ($LASTEXITCODE -ne 0) { throw 'Há testes falhando; a cobertura não foi conferida.' }

    $arquivosDeCobertura = Get-ChildItem -Path 'TestResults' -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $arquivosDeCobertura) {
        Write-Host 'Nenhum arquivo de cobertura foi gerado (sem testes executados).' -ForegroundColor Yellow
        exit 0
    }

    & dotnet reportgenerator '-reports:TestResults/**/coverage.cobertura.xml' '-targetdir:coverage' '-reporttypes:Html;JsonSummary;MarkdownSummaryGithub;Cobertura'
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o relatório de cobertura.' }
}

if (-not (Test-Path $Relatorio)) {
    throw "Relatório não encontrado: $Relatorio. Rode com -ExecutarTestes ou gere o relatório antes."
}

$dados = Get-Content $Relatorio -Raw | ConvertFrom-Json
$projetos = @(Obter-Valor $dados.coverage 'assemblies') | Where-Object { $null -ne $_ -and [int](Obter-Valor $_ 'coverablelines') -gt 0 }

if ($projetos.Count -eq 0) {
    Write-Host 'Sem linhas cobríveis: nada a verificar.' -ForegroundColor Yellow
    exit 0
}

$falhas = [System.Collections.Generic.List[string]]::new()
$linhasDaTabela = foreach ($projeto in $projetos) {
    $linhas = [double](Obter-Valor $projeto 'coverage')
    $ramosTotal = [int](Obter-Valor $projeto 'totalbranches')
    $ramos = if ($ramosTotal -gt 0) { [double](Obter-Valor $projeto 'branchcoverage') } else { $null }

    $situacao = 'OK'
    if ($linhas -lt $Minimo) {
        $situacao = 'ABAIXO (linhas)'
        $falhas.Add("$($projeto.name): linhas $linhas% < $Minimo%")
    }
    if ($null -ne $ramos -and $ramos -lt $Minimo) {
        $situacao = if ($situacao -eq 'OK') { 'ABAIXO (ramos)' } else { 'ABAIXO (linhas e ramos)' }
        $falhas.Add("$($projeto.name): ramos $ramos% < $Minimo%")
    }

    [pscustomobject]@{
        Projeto  = $projeto.name
        Linhas   = "$linhas%"
        Ramos    = if ($null -eq $ramos) { 'sem ramos' } else { "$ramos%" }
        Situacao = $situacao
    }
}

$totalLinhas = [double](Obter-Valor $dados.summary 'linecoverage')
$totalRamosValido = [int](Obter-Valor $dados.summary 'totalbranches')
$totalRamos = if ($totalRamosValido -gt 0) { [double](Obter-Valor $dados.summary 'branchcoverage') } else { $null }

if ($totalLinhas -lt $Minimo) { $falhas.Add("Total: linhas $totalLinhas% < $Minimo%") }
if ($null -ne $totalRamos -and $totalRamos -lt $Minimo) { $falhas.Add("Total: ramos $totalRamos% < $Minimo%") }

$linhasDaTabela | Format-Table -AutoSize
$textoRamos = if ($null -eq $totalRamos) { 'sem ramos' } else { "$totalRamos%" }
Write-Host "Total: linhas $totalLinhas%, ramos $textoRamos (mínimo $Minimo%)"

if ($falhas.Count -gt 0) {
    Write-Host ''
    Write-Host 'Cobertura abaixo do mínimo:' -ForegroundColor Red
    $falhas | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'Cobertura dentro da meta.' -ForegroundColor Green
exit 0
