<#
.SYNOPSIS
    Valida a mensagem de commit: gitmoji + tipo semântico + descrição, sem coautoria.

.DESCRIPTION
    Chamado pelo hook commit-msg do Husky.Net com o caminho do arquivo da mensagem.
    Regras (docs/git-e-commits.md):
      - primeira linha: <gitmoji> <tipo>(<escopo opcional>)<!>: <descrição de até 72 caracteres>;
      - o gitmoji precisa estar na tabela de gitmojis permitidos do guia;
      - a mensagem não pode ter a linha Co-Authored-By;
      - mensagens geradas pelo git (Merge, Revert, fixup! e squash!) são aceitas.
    Termina com código 1 e explica o problema se a mensagem for inválida.

.PARAMETER ArquivoMensagem
    Caminho do arquivo com a mensagem de commit.

.EXAMPLE
    pwsh ./scripts/validar-mensagem-commit.ps1 .git/COMMIT_EDITMSG
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$ArquivoMensagem
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$guia = Join-Path $raiz 'docs/git-e-commits.md'

function Normalizar-Emoji {
    # O seletor de variação U+FE0F aparece em alguns emojis e não em outros; ignora na comparação.
    param([string]$Texto)
    return $Texto.Replace([string][char]0xFE0F, '')
}

function Falhar {
    param([string]$Motivo, [string]$PrimeiraLinha)
    Write-Host ''
    Write-Host "Mensagem de commit inválida: $Motivo" -ForegroundColor Red
    if ($PrimeiraLinha) { Write-Host "  Recebida: $PrimeiraLinha" -ForegroundColor Red }
    Write-Host '  Formato:  <gitmoji> <tipo>(<escopo opcional>): <descrição no imperativo, até 72 caracteres>'
    Write-Host '  Exemplo:  ✨ feat(campanhas): adiciona criação de campanha'
    Write-Host '  Tipos:    feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert'
    Write-Host '  Gitmojis: tabela em docs/git-e-commits.md'
    Write-Host ''
    exit 1
}

if (-not (Test-Path $ArquivoMensagem)) {
    throw "Arquivo da mensagem não encontrado: $ArquivoMensagem"
}

$linhas = @(Get-Content -Path $ArquivoMensagem -Encoding utf8 | Where-Object { $_ -notmatch '^\s*#' })
$primeira = if ($linhas.Count -gt 0) { $linhas[0].TrimEnd() } else { '' }

if ([string]::IsNullOrWhiteSpace($primeira)) { Falhar 'a mensagem está vazia.' '' }

# Mensagens geradas pelo próprio git.
if ($primeira -match '^(Merge |Revert |fixup! |squash! )') { exit 0 }

# Coautoria não é usada neste projeto.
if (($linhas -join "`n") -match '(?im)^\s*Co-Authored-By:') {
    Falhar 'não use a linha Co-Authored-By nos commits deste projeto.' $primeira
}

$padrao = '^(?<emoji>\S+) (?<tipo>feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9-]+\))?!?: (?<descricao>\S.{0,71})$'
$resultado = [regex]::Match($primeira, $padrao)
if (-not $resultado.Success) {
    if ($primeira.Length -gt 100) {
        Falhar 'a primeira linha não segue o formato ou a descrição passa de 72 caracteres.' $primeira.Substring(0, 100)
    }
    Falhar 'a primeira linha não segue o formato <gitmoji> <tipo>(<escopo>): <descrição>.' $primeira
}

# Gitmojis permitidos: primeira coluna da tabela do guia.
$permitidos = @()
if (Test-Path $guia) {
    $permitidos = @(Get-Content -Path $guia -Encoding utf8 |
            ForEach-Object { if ($_ -match '^\| (\S+) \| `[a-z!]+` \|') { Normalizar-Emoji $Matches[1] } })
}

if ($permitidos.Count -eq 0) {
    Write-Host 'Aviso: tabela de gitmojis não encontrada em docs/git-e-commits.md; o gitmoji não foi conferido.' -ForegroundColor Yellow
    exit 0
}

$emoji = Normalizar-Emoji $resultado.Groups['emoji'].Value
if ($permitidos -notcontains $emoji) {
    Falhar "o gitmoji '$($resultado.Groups['emoji'].Value)' não está na tabela de docs/git-e-commits.md." $primeira
}

exit 0
