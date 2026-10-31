# Conexão Solidária

Plataforma de doações para a **ONG Esperança Solidária**, desenvolvida como **Hackathon da Fase 5** da PósTech FIAP — Arquitetura de Sistemas .NET.

O MVP digitaliza a gestão de campanhas e doações com foco em **escalabilidade, observabilidade e automação**, aplicando o conteúdo das 5 fases do curso.

> **Status:** em especificação. O passo a passo de execução abaixo será preenchido conforme as etapas da [especificação](docs/spec/) forem concluídas.

## Funcionalidades

| # | Funcionalidade | Acesso |
|---|---|---|
| 1 | Autenticação JWT com as roles `GestorONG` e `Doador` | — |
| 2 | Gestão de campanhas (meta > 0, data fim não pode estar no passado) | `GestorONG` |
| 3 | Cadastro de doador (e-mail único, CPF validado, senha com hash) | Público |
| 4 | Painel de transparência: campanhas ativas, meta e total arrecadado | Público |
| 5 | Doação para campanha ativa, processada de forma assíncrona | `Doador` |

## Arquitetura (resumo)

- **Microsserviços:** `Identity.Api`, `Campanhas.Api`, `Arrecadacao.Worker` e a função serverless `Notificacoes.Function`.
- **Mensageria:** a doação publica `DoacaoRecebidaEvent` no RabbitMQ (MassTransit). O Worker consome o evento e atualiza o total arrecadado.
- **Dados:** PostgreSQL, MongoDB, Redis e Elasticsearch, cada um com uma justificativa documentada.
- **Infra:** Docker, Kubernetes (Minikube) e Kong como API Gateway.
- **Observabilidade:** OpenTelemetry, Prometheus, Grafana e Jaeger.
- **CI/CD:** GitHub Actions faz build, testes e a imagem Docker a cada push na `main`.
- **Frontend:** Blazor.

Detalhes, diagramas e decisões: [docs/spec/07-arquitetura.md](specs/product/arquitetura.md) e [docs/spec/08-decisoes-arquiteturais.md](docs/spec/08-decisoes-arquiteturais.md).

## Estrutura do repositório

```text
.
├── docs/                 # Especificação, roteiro de vídeo, diagramas e ADRs
├── src/                  # Código-fonte (serviços, worker, função e frontend)
├── tests/                # Testes de unidade, BDD, integração e carga
├── deploy/               # docker-compose, manifestos Kubernetes, Kong e Grafana
└── .github/workflows/    # Pipelines de CI/CD
```

## Como executar localmente

Pré-requisitos em [docs/spec/02-pre-requisitos.md](docs/pre-requisitos.md). O passo a passo de subida está em [docs/spec/13-infraestrutura.md](specs/product/infraestrutura.md) e receberá os comandos finais na Etapa 17 do [plano de execução](docs/plano-de-execucao.md).

## Convenção de commits

Commits semânticos ([Conventional Commits](https://www.conventionalcommits.org/pt-br/)) com [gitmoji](https://gitmoji.dev/):

```text
<gitmoji> <tipo>(<escopo opcional>): <descrição no imperativo>
```

Exemplo: `✨ feat(campanhas): adiciona criação de campanha`. Todo commit deve compilar e passar nos testes. As regras completas estão em [docs/spec/17-git-e-commits.md](docs/git-e-commits.md).

## Documentação

- [Especificação do projeto](docs/spec/): 23 arquivos, dos pré-requisitos à conclusão (começar por [01 — Visão geral](specs/product/overview.md))
- [Manual de implementação](docs/manual-de-implementacao.md): como implantar e operar (docker-compose, Minikube e Azure)
- [Manual do usuário](docs/manual-do-usuario.md): como visitantes, doadores e gestores usam a plataforma
- [Roteiro do vídeo](docs/ROTEIRO-VIDEO.md): apresentação de até 15 minutos

## Equipe

| Nome | Discord | GitHub |
|---|---|---|
| _a preencher_ | | |
