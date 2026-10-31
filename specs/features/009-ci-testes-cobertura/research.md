# Pesquisa — Feature 009 (Testes unitários e cobertura no CI)

[← Spec](spec.md)

## R1 — Framework de testes

- **Decisão:** xUnit v3 com Shouldly (asserções) e NSubstitute (dublês).
- **Justificativa:** um dos três frameworks aceitos pelo enunciado; padrão do projeto; integração com `dotnet test`, Testcontainers e Reqnroll.
- **Alternativas:** MSTest (integrado ao Visual Studio, menos usado com Testcontainers); NUnit (bom, mas exigiria outro estilo de atributos).
- **Fonte:** `wiki:conceitos/frameworks-de-teste-dotnet`, `wiki:conceitos/teste-de-unidade`.

## R2 — Coleta de cobertura

- **Decisão:** `coverlet.collector` (`--collect "XPlat Code Coverage"`) com `coverlet.runsettings`, gerando `coverage.cobertura.xml` por projeto de teste.
- **Justificativa:** caminho padrão do .NET, sem ferramenta paga; formato aceito pelo ReportGenerator, pelo GitHub e pelo Sonar.
- **Alternativas:** `coverlet.msbuild` (configuração por projeto); Microsoft Code Coverage (formato `.coverage` binário).
- **Fonte:** `wiki:conceitos/cobertura-de-codigo`.

## R3 — Como "retornar a cobertura na pipeline"

- **Decisão:** ReportGenerator gera `Html`, `Cobertura`, `JsonSummary` e `MarkdownSummaryGithub`; o Markdown vai para `$GITHUB_STEP_SUMMARY` (visível na página da execução) e o restante vai para o artefato `cobertura-testes-unitarios` (download).
- **Justificativa:** atende literalmente o critério do enunciado ("dados de cobertura com download na pipeline") e fica visível sem abrir o arquivo.
- **Alternativas:** SonarCloud (exige conta e `SONAR_TOKEN`); comentário de cobertura no PR (útil, mas não substitui o artefato).
- **Fonte:** `wiki:conceitos/workflow-github-actions`, `wiki:conceitos/sonarqube`.

## R4 — Separar unidade e integração

- **Decisão:** dois jobs: `testes-unitarios` (sem Docker, `--filter Category=Unit`) e `testes-integracao` (todos os testes, com Docker, e verificação dos 90%).
- **Justificativa:** o enunciado avalia testes unitários; o job de unidade fica rápido e independente de containers. A meta de 90% do projeto considera todos os testes.

## R5 — Plataforma de CI

- **Decisão:** GitHub Actions (`ubuntu-latest`, Docker disponível para Testcontainers).
- **Alternativas:** Azure DevOps Pipelines com `PublishCodeCoverageResults@2` — aceito pelo enunciado, mas exige projeto no Azure DevOps.
- **Fonte:** `wiki:conceitos/github-actions`, `wiki:conceitos/azure-pipelines`.
