# Scripts

Automação do ambiente em **PowerShell 7** (`pwsh`), que roda igual em Windows, macOS e Linux. Execução: `pwsh ./scripts/<nome>.ps1`.

| Script | Uso | Criado em |
|---|---|---|
| `verificar-pre-requisitos.ps1` | Confere as ferramentas da máquina e mostra o comando de instalação do sistema detectado | Etapa 3 |
| `validar-mensagem-commit.ps1` | Valida a mensagem de commit (hook `commit-msg` do Husky.Net) | Etapa 3 |
| `verificar-cobertura.ps1` | Roda os testes com cobertura (`-ExecutarTestes`) e falha abaixo do mínimo (`-Minimo 90`) | Etapa 3 |
| `seed-demo.ps1` | Cria dados de demonstração pela API (`-IdentityUrl`, `-CampanhasUrl`) | Etapa 10 |
| `k8s-secrets.ps1` | Cria os Secrets do Kubernetes a partir do `deploy/.env` (`-EnvFile`) | Etapa 11 |
| `up.ps1` | Sobe o ambiente Kubernetes local completo | Etapa 11 |
| `backup.ps1` | `pg_dump` e `mongodump` antes de demonstrações | Etapa 11 |

## Regras

- `Set-StrictMode -Version Latest` e `$ErrorActionPreference = 'Stop'` no início de cada script.
- Parâmetros nomeados com `param()` e ajuda (`.SYNOPSIS`, `.EXAMPLE`) no topo.
- Nenhum segredo no script; valores vêm de `deploy/.env` ou de parâmetros.
- Detectar o sistema com `$IsWindows`, `$IsMacOS` e `$IsLinux` quando o comportamento mudar.

Etapas: [plano de execução](../docs/plano-de-execucao.md).
