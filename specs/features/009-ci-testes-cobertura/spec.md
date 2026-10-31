# Feature 009 — Testes unitários e cobertura no CI

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/009-ci-testes-cobertura` |
| Status | Especificada |
| Origem | Atividade Substitutiva da Fase 3 (enunciado completo; [ADR 0018](../../../docs/adr/0018-escopo-trabalho-substitutivo.md)) |
| Histórias | H11, H12 |
| Prioridade | **P0 — requisito avaliado** |
| Componentes | Projetos `tests/unit/*`, workflow `.github/workflows/ci.yml` |
| Depende de | Etapa 3 do [plano de execução](../../../docs/plano-de-execucao.md) (solução e projetos de teste); cresce a cada feature 001–008 |

## Objetivo

Assegurar a qualidade de software da API: testes unitários das funcionalidades, com casos de sucesso e de erro, executados numa pipeline de CI que mostra a cobertura de código e permite baixar o relatório.

## Enunciado (resumo fiel)

1. Escolher uma API .NET com funcionalidades testáveis (métodos, serviços ou controladores).
2. Implementar testes unitários das funcionalidades selecionadas com MSTest, NUnit ou xUnit.
3. Cobrir casos de sucesso e cenários de erro.
4. Configurar uma pipeline de CI (GitHub Actions ou Azure DevOps) que rode os testes unitários e retorne a cobertura.
5. Critério: a pipeline executa os testes e retorna a cobertura (Sonar ou dados de cobertura para download na pipeline).
6. Entregável: vídeo com todos os passos funcionando; não precisa 100% de cobertura, mas precisa ter mais de um teste.

## Histórias de usuário

> **H11** — Como avaliador, quero ver a pipeline executar os testes unitários e mostrar a cobertura, para confirmar a qualidade da API.
>
> **H12** — Como desenvolvedor, quero que a pipeline falhe quando um teste falhar ou a cobertura cair, para não integrar código sem qualidade.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Testes unitários e cobertura no CI

  Cenário: Pipeline em push na main
    Dado um commit enviado para a branch "main"
    Quando a pipeline "CI" é executada no GitHub Actions
    Então o job "testes-unitarios" executa todos os testes com a categoria "Unit"
    E o resumo da execução mostra a cobertura de linhas e de ramos por projeto
    E o artefato "cobertura-testes-unitarios" fica disponível para download

  Cenário: Pipeline em pull request
    Dado um pull request para a branch "main"
    Quando a pipeline "CI" é executada
    Então os mesmos testes e o mesmo relatório de cobertura são gerados

  Cenário: Execução manual para demonstração
    Quando alguém aciona a pipeline "CI" pelo botão "Run workflow"
    Então a pipeline executa os testes e publica a cobertura

  Cenário: Teste falhando
    Dado um teste unitário que falha
    Quando a pipeline é executada
    Então o job "testes-unitarios" termina com erro
    E o relatório de testes mostra qual teste falhou

  Cenário: Cobertura abaixo do mínimo
    Dado que a cobertura total ficou abaixo de 90%
    Quando a pipeline é executada
    Então o job "testes-integracao" termina com erro e lista os projetos abaixo da meta

  Cenário: Sucesso e erro cobertos
    Dado uma funcionalidade da API com regra de validação
    Então existe ao menos um teste de caso de sucesso
    E existe ao menos um teste de cenário de erro para cada regra
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | A API testada DEVE ser a Conexão Solidária (.NET 10), com funcionalidades em métodos de domínio, serviços (casos de uso) e endpoints (features 001–008). |
| FR-002 | Os testes unitários DEVEM usar **xUnit** (framework de testes do .NET), com o atributo `[Trait("Category", "Unit")]`. |
| FR-003 | Cada funcionalidade testada DEVE ter casos de **sucesso** e **cenários de erro** (validação 400, regra de negócio 422, autorização 401/403, não encontrado 404, conflito 409, falha de dependência). |
| FR-004 | A suíte DEVE ter mais de um teste; a meta do projeto é cobrir todas as regras das specs. |
| FR-005 | A pipeline DEVE rodar no **GitHub Actions** em `push` e `pull_request` para a `main` e por acionamento manual (`workflow_dispatch`). |
| FR-006 | A pipeline DEVE executar os testes unitários e coletar a cobertura (coverlet, formato Cobertura). |
| FR-007 | A pipeline DEVE **retornar a cobertura**: resumo em Markdown na página da execução e artefato `cobertura-testes-unitarios` (HTML navegável + `Cobertura.xml` + `Summary.json`) para download. |
| FR-008 | A pipeline DEVE publicar os resultados dos testes (`.trx`) como artefato e falhar se algum teste falhar. |
| FR-009 | Um segundo job DEVE rodar todos os testes (unidade, contrato, integração e aceitação), publicar o artefato `cobertura-total` e falhar se a cobertura total ou de algum projeto de `src/` ficar abaixo de 90% ([constituição](../../constitution.md), princípio IV). |
| FR-010 | Os artefatos DEVEM ficar disponíveis por pelo menos 14 dias. |

## Casos de borda

- Primeira execução, antes de existir código de produção: a pipeline roda, informa "0 testes" e não falha por cobertura.
- Teste instável (*flaky*): é corrigido ou removido; não se usa *retry* automático de teste na pipeline.
- Testes de integração exigem Docker: ficam no segundo job, para que o job de unidade continue rápido e independente.

## Entidades-chave

- **Relatório de cobertura** e **resultado de testes**: artefatos da execução ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | Toda execução da pipeline na `main` mostra a cobertura no resumo e oferece o artefato para download. |
| SC-002 | Job `testes-unitarios` termina em menos de 5 minutos. |
| SC-003 | Cobertura total ≥ 90% de linhas e ramos (meta do projeto; o enunciado não exige percentual). |
| SC-004 | Todas as features 001–008 têm testes de sucesso e de erro (verificado no `/analyze` e na revisão). |
| SC-005 | O vídeo mostra: testes passando localmente, pipeline executando, resumo de cobertura e download do artefato. |

## Fora do escopo

Azure DevOps e SonarCloud (alternativas registradas no [ADR 0018](../../../docs/adr/0018-escopo-trabalho-substitutivo.md)); testes de mutação.
