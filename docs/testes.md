# 15 — Testes

[← README](../README.md) · Anterior: [14 — CI/CD](../specs/product/ci-cd.md) · Próximo: [16 — Padrões de código](padroes-de-codigo.md)

Referências: `wiki:disciplinas/qualidade-software`, `wiki:disciplinas/principios-qualidade`, `wiki:conceitos/piramide-de-testes`, `wiki:conceitos/tdd`, `wiki:conceitos/bdd`, `wiki:conceitos/teste-de-carga`.

## 15.1 Estratégia (pirâmide)

| Nível | Quantidade | Ferramentas | O que cobre | Roda em |
|---|---|---|---|---|
| Unidade | Muitos | xUnit v3, Shouldly, NSubstitute | Domínio (agregados, VOs), casos de uso, validadores, handlers | Todo commit (hook) e CI |
| Arquitetura | Poucos | NetArchTest | Regras de dependência entre camadas | CI |
| Contrato de eventos | Poucos | xUnit + snapshot JSON | Serialização dos eventos v1 | CI |
| Integração | Médio | `WebApplicationFactory`, Testcontainers (PostgreSQL, RabbitMQ, Redis, MongoDB, Elasticsearch), MassTransit Test Harness | Endpoints reais, banco real, Outbox, consumidores | CI e `pre-push` |
| Aceitação (BDD) | Um por cenário Gherkin | Reqnroll + xUnit | Critérios de aceitação dos RFs | CI |
| Componentes de UI | Poucos | bUnit | Componentes Blazor | CI |
| E2E (bônus) | Muito poucos | Playwright | Jornada "doar" no navegador | Manual / agendado |
| Carga | Poucos | k6 | RNF01–RNF04 | Manual, antes do vídeo |

Um framework de teste para toda a solução: **xUnit v3**.

## 15.2 Convenções

- Nome: `Metodo_Cenario_ResultadoEsperado` (ex.: `Criar_DataFimNoPassado_RetornaErroDataFimPassada`).
- Padrão **Arrange–Act–Assert**, uma ação por teste.
- Datas controladas com `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`); nunca `DateTime.Now`.
- Builders para dados de teste (`CampanhaBuilder`, `DoadorBuilder`); CPFs válidos de teste gerados por função, nunca reais.
- Testes independentes; integração com banco limpo por classe (Respawn ou banco por fixture).
- Sem `Thread.Sleep`; esperas com timeout e polling (ex.: aguardar o Worker).
- Mocks só para dependências externas ou lentas; nunca para o domínio.

## 15.3 TDD

Obrigatório (teste antes do código, ciclo vermelho → verde → refatorar) em:

| Alvo | Casos mínimos |
|---|---|
| `Cpf` | Válido com e sem máscara; dígito errado; sequência repetida; tamanho errado; `Mascarado()` |
| `Email` | Válido; normalização; inválido; tamanho |
| `Senha` | Cada regra da política |
| `Dinheiro` | Arredondamento; negativo rejeitado; soma |
| `Periodo` | Fim antes do início |
| `Campanha` | Criar válida; data fim passada; meta 0 e negativa; editar não ativa; concluir; cancelar; transições inválidas; `PodeReceberDoacao` (antes do início, no período, depois do fim, encerrada); `RegistrarDoacaoProcessada`; `PercentualAtingido` |
| `Usuario` | Cadastro sem consentimento; anonimizar; anonimizar duas vezes; senha após anonimizar |

Como todo commit precisa compilar e passar nos testes, o teste e a implementação mínima que o faz passar vão **no mesmo commit** (`✨ feat`). A refatoração do ciclo vai em commit separado (`♻️ refactor`). O teste falhando existe só na máquina de quem desenvolve, nunca no histórico.

## 15.4 Testes de integração obrigatórios

| Serviço | Cenário |
|---|---|
| Identity.Api | Cadastro 201; e-mail duplicado 409; CPF duplicado 409; login 200 com token válido; login 401; bloqueio 429; `/me` 200; anonimizar 204 e login seguinte 401 |
| Campanhas.Api | CRUD com gestor; 401 sem token; 403 com doador; 422 regras; 409 concorrência (`If-Match`); painel só ativas; cache invalidado após alteração |
| Campanhas.Api (doação) | 202 grava mensagem no Outbox; 422 não grava; `Idempotency-Key` repetida devolve o mesmo `doacaoId` |
| Campanhas.Api (busca) | Indexa ao criar; encontra com erro de digitação; ignora cancelada; 503 com Elasticsearch parado |
| Arrecadacao.Worker | Processa e soma; evento duplicado soma uma vez; grava no MongoDB; publica `DoacaoProcessadaEvent`; falha transitória é reprocessada; falha permanente vai para `_error` |
| Notificacoes.Function | Lê o envelope do MassTransit e registra o recibo sem dados pessoais |
| Ponta a ponta (BDD) | Doação → Worker → painel com total atualizado em até 5 s |

## 15.5 BDD (Reqnroll)

- Projeto `tests/ConexaoSolidaria.Bdd` com `Features/*.feature` em português (`# language: pt`).
- Um arquivo por RF, com os cenários de [04 — Requisitos funcionais](spec/04-requisitos-funcionais.md).
- Steps declarativos ("o doador doa 150.00"), sem detalhes de HTTP no texto.
- Steps chamam as APIs reais em memória (`WebApplicationFactory`) com infraestrutura em Testcontainers.
- Relatório de execução publicado como artefato do CI.

## 15.6 Testes de carga (k6)

Pasta `tests/load/`, execução contra o Kong local:

| Script | Cenário | Thresholds |
|---|---|---|
| `painel.js` | `ramping-vus` até 50 VUs por 2 min em `GET /transparencia/campanhas` | `http_req_duration{p(95)}<300`, `http_req_failed<0.01` |
| `doacao.js` | 20 VUs, cada um com token de doador, `POST /doacoes` | `p(95)<500`, `http_req_failed<0.01` |
| `busca.js` | 30 VUs com termos com erro de digitação | `p(95)<400` |

- `checks` validam status e corpo.
- Resultado salvo em `tests/load/results/` (ignorado pelo git) e resumo no README.
- Durante a carga, observar no Grafana a subida de réplicas (HPA/KEDA): bom material para o vídeo.

## 15.7 Cobertura (mínimo obrigatório de 90%)

| Item | Definição |
|---|---|
| Meta | **≥ 90% de linhas e ≥ 90% de ramos**, no total da solução **e** em cada projeto de `src/` ([RNF34](../specs/product/requisitos-nao-funcionais.md)) |
| Coleta | `coverlet.collector` (`--collect "XPlat Code Coverage"`) com `--settings coverlet.runsettings` |
| Soma | Unidade, integração, BDD e bUnit somados pelo ReportGenerator em um único `coverage/Summary.json` |
| Relatório | HTML como artefato do CI; resumo Markdown no `GITHUB_STEP_SUMMARY` |
| Verificação | `scripts/verificar-cobertura.ps1 -Minimo 90` lê o `Summary.json` e termina com erro (exit 1) se o total ou algum projeto ficar abaixo de 90% em linhas ou ramos, listando quais |
| Onde é obrigatória | Hook `pre-push` ([17.4](git-e-commits.md)) e job `build-test` do CI; PR com cobertura abaixo da meta não faz merge |
| Fora da medição | Projetos de teste, `**/Migrations/*.cs`, `**/Program.cs`, código gerado (`*.g.cs`, `GeneratedCodeAttribute`, arquivos do Reqnroll) |
| `[ExcludeFromCodeCoverage]` | Só com `Justification` preenchida e aprovada no PR; nunca para fugir da meta |

### Arquivo `coverlet.runsettings` (raiz, criado na Etapa 3)
```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Include>[ConexaoSolidaria.*]*,[Identity.*]*,[Campanhas.*]*,[Arrecadacao.*]*,[Notificacoes.*]*</Include>
          <Exclude>[*.UnitTests]*,[*.IntegrationTests]*,[*.Tests]*,[ConexaoSolidaria.Bdd]*</Exclude>
          <ExcludeByFile>**/Migrations/*.cs,**/Program.cs,**/*.g.cs</ExcludeByFile>
          <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute,ExcludeFromCodeCoverageAttribute</ExcludeByAttribute>
          <SkipAutoProps>true</SkipAutoProps>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### Rodar localmente (Windows, macOS e Linux)
```bash
dotnet test -c Release --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;JsonSummary;MarkdownSummaryGithub"
pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90
```
Relatório navegável em `coverage/index.html` (pasta ignorada pelo git).

### Como chegar aos 90% em cada projeto

| Projeto | Principal fonte de cobertura |
|---|---|
| `*.Domain` | Testes de unidade com TDD (15.3) |
| `*.Application` | Testes de unidade dos casos de uso com fakes das interfaces |
| `*.Infrastructure` | Testes de integração com Testcontainers (repositórios, Outbox, cifragem, Elasticsearch, MongoDB, Redis) |
| `*.Api` | Testes de integração com `WebApplicationFactory` e cenários BDD (todos os códigos de status de [09](../specs/product/api-e-eventos.md)) |
| `Arrecadacao.Worker` | MassTransit Test Harness + Testcontainers (sucesso, duplicata, falha transitória, falha permanente) |
| `Notificacoes.Function` | Testes de unidade da função (envelope, deduplicação, log mascarado) |
| `ConexaoSolidaria.Web` | bUnit em todos os componentes e páginas (estados de carregando, sucesso, erro e sem permissão) |
| `ConexaoSolidaria.Contracts`, `ServiceDefaults` | Testes de snapshot dos eventos e testes de integração dos health checks e do tratamento de erros |

Ramos (`if`, `switch`, `?:`, `??`) contam: cada regra de validação e cada caminho de erro precisa de um teste.

## 15.8 Testes de regressão

Todo bug corrigido ganha um teste que o reproduz, no mesmo commit da correção (`🐛 fix(...)`).
