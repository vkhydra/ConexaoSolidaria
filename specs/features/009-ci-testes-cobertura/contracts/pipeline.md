# Contrato da pipeline — Feature 009

[← Spec](../spec.md)

Contrato do workflow `.github/workflows/ci.yml` com quem o usa (avaliador, PR e proteção da `main`).

## Gatilhos

| Evento | Filtro | Uso |
|---|---|---|
| `push` | branch `main`, tags `v*` | Integração contínua |
| `pull_request` | branch `main` | Validação antes do merge |
| `workflow_dispatch` | — | Execução manual (gravação do vídeo) |

## Jobs e checks obrigatórios

| Job (check) | Depende de | Falha quando | Saída |
|---|---|---|---|
| `testes-unitarios` | — | Build com erro ou aviso; teste unitário falhando | Resumo de cobertura; artefatos `cobertura-testes-unitarios` e `resultados-testes-unitarios` |
| `testes-integracao` | `testes-unitarios` | Teste falhando; cobertura total ou de algum projeto < 90% | Artefatos `cobertura-total` e `resultados-testes` |

Os demais jobs do CI (varredura de segredos, imagens Docker, CodeQL) estão em [CI/CD](../../../product/ci-cd.md).

## Comandos do job `testes-unitarios`

```bash
dotnet restore --locked-mode
dotnet build -c Release --no-restore -warnaserror
dotnet test -c Release --no-build --filter Category=Unit \
  --settings coverlet.runsettings --collect "XPlat Code Coverage" \
  --logger trx --results-directory TestResults/unit
dotnet reportgenerator -reports:"TestResults/unit/**/coverage.cobertura.xml" \
  -targetdir:coverage/unit -reporttypes:"Html;Cobertura;JsonSummary;MarkdownSummaryGithub" \
  -title:"Cobertura — testes unitários"
cat coverage/unit/SummaryGithub.md >> "$GITHUB_STEP_SUMMARY"
```

## Garantias

- O resumo e os artefatos são publicados também quando um teste falha (`if: always()`), para facilitar o diagnóstico.
- Nenhum segredo é necessário para estes jobs.
- Mesmos comandos rodam localmente (skill `verificar-cobertura` e `scripts/verificar-cobertura.ps1`).
