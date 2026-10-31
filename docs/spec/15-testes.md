# 15 — Testes

[← README](../../README.md) · Anterior: [14 — CI/CD](14-ci-cd.md) · Próximo: [16 — Padrões de código](16-padroes-de-codigo.md)

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
- Um arquivo por RF, com os cenários de [04 — Requisitos funcionais](04-requisitos-funcionais.md).
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

## 15.7 Cobertura

| Item | Definição |
|---|---|
| Coleta | `coverlet.collector` (`--collect "XPlat Code Coverage"`) |
| Relatório | ReportGenerator (HTML como artefato; resumo Markdown no `GITHUB_STEP_SUMMARY`) |
| Meta | ≥ 80% de linhas e ramos em `*.Domain` e `*.Application` (indicador; queda relevante é discutida no PR) |
| Exclusões | `Migrations`, `Program.cs`, código gerado (`[ExcludeFromCodeCoverage]` só com justificativa) |

## 15.8 Testes de regressão

Todo bug corrigido ganha um teste que o reproduz, no mesmo commit da correção (`🐛 fix(...)`).
