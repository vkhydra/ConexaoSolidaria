# 14 — CI/CD (GitHub Actions)

[← README](../../README.md) · Anterior: [13 — Infraestrutura](infraestrutura.md) · Próximo: [15 — Testes](../../docs/testes.md)

Atende ao requisito obrigatório "pipeline acionada a cada push na branch principal que faz .NET build e gera a imagem Docker" e ao bônus "testes de unidade dentro do CI". Referências: `wiki:disciplinas/github-actions`, `wiki:boas-praticas/github-actions`, `wiki:conceitos/ci-cd`.

## 14.1 Workflows

| Arquivo | Gatilho | Objetivo |
|---|---|---|
| `.github/workflows/ci.yml` | `push` e `pull_request` na `main`; tags `v*`; `workflow_dispatch` | Formato, build, testes, cobertura, scans, imagens Docker |
| `.github/workflows/codeql.yml` | `push`/`pull_request` na `main` e semanal | Análise estática de segurança (C#) |
| `.github/workflows/cd.yml` | `workflow_run` do CI concluído com sucesso na `main` + `workflow_dispatch` | **Opcional**: deploy no AKS |
| `.github/dependabot.yml` | Semanal | Atualizar NuGet, GitHub Actions e imagens Docker |

## 14.2 Pipeline de CI

```mermaid
flowchart LR
  A[push / PR] --> B[build-test]
  A --> C[secret-scan]
  B --> D[docker<br/>matriz por serviço]
  C --> D
  D --> E{push na main<br/>ou tag?}
  E -- sim --> F[push no GHCR]
  E -- não --> G[só build e scan]
```

### Job `build-test`
| Passo | Comando / action | Falha quando |
|---|---|---|
| Checkout | `actions/checkout` | — |
| SDK | `actions/setup-dotnet` com `global-json-file: global.json` e cache de NuGet por `packages.lock.json` | — |
| Ferramentas | `dotnet tool restore` | — |
| Restore | `dotnet restore --locked-mode` | Lock file desatualizado |
| Formato | `dotnet format --verify-no-changes --no-restore` | Código fora do `.editorconfig` |
| Build | `dotnet build -c Release --no-restore -warnaserror` | Erro ou aviso |
| Pacotes vulneráveis | `dotnet list package --vulnerable --include-transitive` + verificação da saída | Vulnerabilidade alta ou crítica |
| Testes | `dotnet test -c Release --no-build --settings coverlet.runsettings --collect "XPlat Code Coverage" --logger trx --results-directory TestResults` | Teste falhando (Testcontainers usa o Docker do runner) |
| Relatório de cobertura | `dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;JsonSummary;MarkdownSummaryGithub"` e `cat coverage/SummaryGithub.md >> $GITHUB_STEP_SUMMARY` | — |
| Cobertura mínima | `pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90` | Linhas ou ramos abaixo de 90% no total ou em algum projeto de `src/` |
| Artefatos | `actions/upload-artifact` (`TestResults`, `coverage`) com `retention-days: 14` | — |

### Job `secret-scan`
`actions/checkout` com `fetch-depth: 0` + gitleaks. Falha se houver segredo em qualquer commit.

### Job `docker` (matriz)
| Item | Definição |
|---|---|
| Matriz | `identity-api`, `campanhas-api`, `arrecadacao-worker`, `notificacoes-function`, `web` (+ `migrator-identity`, `migrator-campanhas`) |
| Build | `docker/setup-buildx-action` + `docker/build-push-action` com `cache-from/to: type=gha` |
| Tags | `docker/metadata-action`: `sha-<7 caracteres>`; em tag git `vX.Y.Z`, também `X.Y.Z` e `X.Y` |
| Labels OCI | `org.opencontainers.image.source`, `revision`, `version` |
| Scan | Trivy na imagem carregada localmente (`severity: CRITICAL`, `ignore-unfixed: true`, `exit-code: 1`); SARIF enviado para a aba Security |
| Push | Só se `github.event_name == 'push'` (na `main` ou tag); em PR a imagem é construída e escaneada, mas não publicada |
| Registry | `ghcr.io/<owner>/conexao-solidaria/<servico>`; login com `GITHUB_TOKEN` |

### Modelo de `ci.yml` (trecho)
```yaml
name: CI
on:
  push: { branches: [main], tags: ['v*'] }
  pull_request: { branches: [main] }
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build-test:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@<sha>        # v4
      - uses: actions/setup-dotnet@<sha>    # v4
        with:
          global-json-file: global.json
          cache: true
          cache-dependency-path: '**/packages.lock.json'
      - run: dotnet tool restore
      - run: dotnet restore --locked-mode
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet build -c Release --no-restore -warnaserror
      - run: dotnet test -c Release --no-build --settings coverlet.runsettings --collect "XPlat Code Coverage" --logger trx --results-directory TestResults
      - run: dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;JsonSummary;MarkdownSummaryGithub"
      - run: cat coverage/SummaryGithub.md >> "$GITHUB_STEP_SUMMARY"
      - run: pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90
      # ... upload dos artefatos (if: always())

  docker:
    needs: [build-test, secret-scan]
    runs-on: ubuntu-latest
    timeout-minutes: 30
    permissions:
      contents: read
      packages: write
      security-events: write
    strategy:
      fail-fast: false
      matrix:
        servico: [identity-api, campanhas-api, arrecadacao-worker, notificacoes-function, web]
    steps:
      # checkout, buildx, login (só em push), metadata, build (load), trivy, push
```

`<sha>` = SHA completo do commit da versão da action (fixação por SHA, com a versão em comentário).

## 14.3 Regras de segurança do pipeline

- `permissions: contents: read` no topo; permissões extras só no job que precisa.
- Actions de terceiros fixadas por SHA; Dependabot mantém atualizadas.
- Sem `pull_request_target`.
- Nenhum segredo impresso; GHCR usa `GITHUB_TOKEN` (sem token pessoal).
- `concurrency` cancela execuções obsoletas da mesma branch.
- `timeout-minutes` em todo job.

## 14.4 Proteção da `main`

Configurada na Etapa 14 (`gh api` ou interface do GitHub):

- PR obrigatório para merge, com 1 aprovação.
- Checks obrigatórios: `build-test`, `secret-scan`, `docker` (todas as entradas da matriz), `CodeQL`.
- Branch atualizada antes do merge.
- Sem force push e sem exclusão.
- Merge por **rebase** (preserva os commits atômicos, cada um já validado; ver [17 — Git e commits](../../docs/git-e-commits.md)).
- Secret scanning e push protection ligados.

## 14.5 Releases

1. Ao fim da Etapa 19: `git tag -a v1.0.0 -m "🔖 versão 1.0.0"` e `git push origin v1.0.0`.
2. O CI publica as imagens `1.0.0`.
3. `gh release create v1.0.0 --generate-notes` com links para o vídeo e a documentação.

Versionamento semântico: `MAJOR` para contrato incompatível (API ou evento), `MINOR` para funcionalidade, `PATCH` para correção.

## 14.6 CD opcional (AKS)

| Passo | Definição |
|---|---|
| Ambiente | GitHub environment `azure` com aprovação manual |
| Autenticação | `azure/login` com **OIDC** (`permissions: id-token: write`), sem senha de service principal |
| Imagens | Retag do GHCR para o ACR (`az acr import`) ou build direto para o ACR |
| Credenciais do cluster | `az aks get-credentials` |
| Atualização | `kustomize edit set image` no overlay `azure` + `kubectl apply -k` |
| Migrations | Job de migration antes do rollout, aguardado com `kubectl wait` |
| Rolling Update | `kubectl rollout status deploy/<nome> --timeout=5m` para cada serviço |
| Smoke test | `curl -f https://<ip>/api/v1/transparencia/campanhas` |
| Rollback | Em falha: `kubectl rollout undo deploy/<nome>` |

## 14.7 Demonstração no vídeo

1. Alterar um texto pequeno, commit no padrão (`📝 docs: ...`) e push na `main`.
2. Mostrar o workflow rodando: jobs, testes, resumo de cobertura.
3. Mostrar a imagem nova com a tag `sha-<commit>` no GHCR.
