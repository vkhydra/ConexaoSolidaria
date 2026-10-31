# Plano técnico — Feature 009 (Testes unitários e cobertura no CI)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato da pipeline](contracts/pipeline.md) · [Tarefas](tasks.md)

## Resumo

Projetos de testes unitários com xUnit para cada camada da API e workflow do GitHub Actions com dois jobs: `testes-unitarios` (cobertura dos testes unitários, publicada no resumo e como artefato para download) e `testes-integracao` (todos os testes e verificação do mínimo de 90%). A pipeline nasce na Etapa 3, antes das features, e cada feature acrescenta testes a ela.

## Contexto técnico

| Item | Valor |
|---|---|
| Ferramentas | xUnit v3, Shouldly, NSubstitute, `coverlet.collector`, ReportGenerator (ferramenta local), GitHub Actions |
| Configuração | `coverlet.runsettings`, `.config/dotnet-tools.json`, `.github/workflows/ci.yml` |
| Ambiente de CI | `ubuntu-latest` com .NET 10 (`global.json`) e Docker |
| Metas | Job de unidade < 5 min; cobertura total ≥ 90% |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Enunciado do substitutivo traduzido em FRs e cenários |
| II. Domínio | ✅ | Testes de unidade do domínio sem infraestrutura |
| III. TDD | ✅ | Testes escritos antes do código em todas as features |
| IV. Cobertura ≥ 90% | ✅ | Verificada no job `testes-integracao` e no `pre-push` |
| V. `<summary>` | ✅ | Classes e métodos de teste documentados com o cenário |
| VI. Commits | ✅ | Um commit por arquivo de configuração e de teste |
| VII. Segurança | ✅ | `permissions: contents: read`; actions fixadas por SHA; nenhum segredo |
| VIII. Eventos | N/A | — |
| IX. Observabilidade | ✅ | Resumo de cobertura e `.trx` em cada execução |
| X. Infra como código | ✅ | Workflow versionado |
| XI. Simplicidade | ✅ | Artefato em vez de serviço externo (Sonar) |
| XII. Português | ✅ | Nomes de jobs, artefatos e títulos em português |

## Estrutura

```text
.github/workflows/ci.yml                     # jobs testes-unitarios e testes-integracao (+ demais jobs do CI)
coverlet.runsettings                         # filtros e exclusões
.config/dotnet-tools.json                    # dotnet-reportgenerator-globaltool
scripts/verificar-cobertura.ps1              # mesma verificação localmente
tests/unit/{Identity,Campanhas,Arrecadacao.Worker,Notificacoes.Function}.UnitTests/
tests/unit/ConexaoSolidaria.Web.UnitTests/
```

## Design dos testes unitários

| Camada | O que testar | Sucesso | Erro |
|---|---|---|---|
| Domínio (agregados, VOs) | Regras e invariantes | Criar campanha válida; CPF válido | Meta 0; data fim passada; CPF com dígito errado; transição inválida |
| Aplicação (casos de uso) | Orquestração com dublês | Doação aceita grava evento | Campanha inexistente (404); não apta (422); e-mail duplicado (409) |
| Validadores | Formato da entrada | Entrada válida | Cada campo inválido (400) |
| Endpoints | Tradução HTTP ↔ caso de uso | Status 200/201/202/204 | `ProblemDetails` com `code` |
| Worker e função | Consumidor e leitor de envelope | Evento processado | Duplicata; envelope inválido |
| Blazor (bUnit) | Componentes e páginas | Renderização com dados | Mensagem de erro e estado sem permissão |

## Riscos

| Risco | Mitigação |
|---|---|
| Cobertura de unidade baixa em projetos de infraestrutura | Esses projetos são cobertos pelo job `testes-integracao`; o enunciado não exige percentual |
| Pipeline lenta | Cache do NuGet por `packages.lock.json`; unidade e integração em jobs separados |
