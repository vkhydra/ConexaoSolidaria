# Visão geral do produto

[← README](../../README.md) · [Constituição](../constitution.md)

Ponto de partida da especificação: o problema, os objetivos, quem usa e o que está dentro e fora do escopo. Os detalhes de cada funcionalidade estão em [`specs/features/`](../features/); as decisões transversais, nos demais arquivos desta pasta.

## Contexto

Este repositório é a **Atividade Substitutiva da Fase 3** da PósTech FIAP (Arquitetura de Sistema .NET com Azure). O enunciado pede para **assegurar a qualidade de software de uma API .NET**: testes unitários das funcionalidades (sucesso e erro) e uma **pipeline de CI que execute os testes e retorne a cobertura**. O entregável é um vídeo ([ADR 0018](../../docs/adr/0018-escopo-trabalho-substitutivo.md)).

A API escolhida é a **Conexão Solidária**, plataforma de doações da ONG fictícia **Esperança Solidária**, que acolhe crianças em situação de vulnerabilidade. Hoje a ONG controla doadores e campanhas em planilhas e mensagens, o que causa:

- falta de transparência sobre quanto cada campanha arrecadou;
- retrabalho para cadastrar doadores e conferir doações;
- nenhuma visibilidade de uso, erros ou desempenho.

O grupo também aplica na API o conteúdo das 5 fases do curso ([cobertura das fases](../../docs/cobertura-das-fases.md)).

Fontes: `C:\Victor\Fiapiap-pdfs\Trabalho Substitutivoase 3.pdf` e `wiki:fontes/tech-challenge/06-trabalho-substitutivo-fase-3-testes-e-cobertura-ci`.

## Objetivos

| # | Objetivo | Como é medido |
|---|---|---|
| O1 | Atender todos os requisitos do trabalho substitutivo | [Feature 009](../features/009-ci-testes-cobertura/spec.md) e checklist em [entrega e riscos](../../docs/entrega-e-riscos.md) 100% marcados |
| O2 | Aplicar o conteúdo das 5 fases do curso | Matriz em [cobertura das fases](../../docs/cobertura-das-fases.md) |
| O3 | Seguir as boas práticas da LLMWiki | Checklists de `specs/product/` e `docs/` marcados |
| O4 | Manter todo commit compilável e no padrão | Hooks locais e CI verdes ([git e commits](../../docs/git-e-commits.md)) |
| O5 | Demonstrar em vídeo os testes e a pipeline com cobertura | [Roteiro do vídeo](../../docs/ROTEIRO-VIDEO.md) |

## Personas

| Persona | Quem é | O que precisa |
|---|---|---|
| **GestorONG** | Funcionário da ONG que administra campanhas | Criar, editar, concluir e cancelar campanhas; ver doações |
| **Doador** | Pessoa física que contribui | Cadastrar-se, entrar, doar e ver os próprios dados |
| **Visitante** | Qualquer pessoa, sem login | Ver campanhas ativas, quanto foi arrecadado e buscar campanhas |
| **Operador da plataforma** | Equipe técnica (o grupo) | Implantar, monitorar e diagnosticar a plataforma |

## Escopo

### Dentro do escopo (exigido pelo trabalho substitutivo)

1. Uma API .NET com funcionalidades testáveis (métodos, serviços e controladores): a Conexão Solidária.
2. Testes unitários com framework do .NET (xUnit) das funcionalidades da API.
3. Casos de sucesso e cenários de erro em cada funcionalidade testada.
4. Pipeline de CI no GitHub Actions que executa os testes unitários e retorna a cobertura (resumo e artefato para download).
5. Vídeo demonstrando todos os passos funcionando.

Especificação: [feature 009](../features/009-ci-testes-cobertura/spec.md).

### Dentro do escopo (funcionalidades da API testada)

6. Autenticação JWT com as roles `GestorONG` e `Doador` (001).
7. Gestão de campanhas restrita a `GestorONG` (002).
8. Cadastro público de doador (003).
9. Painel público de transparência com o total arrecadado (004).
10. Doação por doador autenticado, processada de forma assíncrona por um Worker (005).
11. Busca de campanhas tolerante a erros de digitação (006).
12. Recibo de doação por função serverless (007).
13. Direitos do titular — LGPD (008).

### Dentro do escopo (aplicação do conteúdo das 5 fases, por escolha do projeto)

14. Microsserviços por bounded context e mensageria com RabbitMQ — Fases 1 e 2.
15. Docker e Kubernetes local com os manifestos `.yaml` — Fase 2.
16. API Gateway (Kong), função serverless com KEDA e observabilidade (Prometheus, Grafana, Jaeger) — Fase 3.
17. Persistência poliglota (PostgreSQL, MongoDB, Redis, Elasticsearch) e CI/CD com imagens Docker — Fase 4.
18. Frontend Blazor, LGPD, SOLID, processo ágil e assistente de IA opcional — Fase 5.
19. Deploy opcional na Azure — Fases 2 e 4.

Os itens 14 a 19 não são avaliados pelo enunciado; podem ser cortados sem afetar a entrega ([ordem de corte](../../docs/entrega-e-riscos.md)).

### Fora do escopo

- Pagamento real (cartão, Pix). A doação é registrada como intenção já confirmada.
- Envio real de e-mail. O recibo é simulado em log.
- Recuperação de senha, refresh token e login social.
- Ambiente de produção permanente na cloud.
- Aplicativo mobile.

## Premissas

- Cada integrante usa Windows, macOS ou Linux, com 16 GB de RAM ou mais para rodar o cluster local ([Pré-requisitos](../../docs/pre-requisitos.md)).
- O repositório será **público** no GitHub.
- A doação é considerada paga no momento do envio (sem gateway de pagamento no escopo).
- O fuso horário de referência para regras de data é `America/Sao_Paulo`.
- Valores monetários em reais (BRL), com 2 casas decimais.

## Catálogo de features

| # | Feature | Origem | Histórias | Serviço | Prioridade |
|---|---|---|---|---|---|
| 001 | [Autenticação e autorização](../features/001-autenticacao/spec.md) | RF01 | H02 | Identity.Api | P1 |
| 002 | [Gestão de campanhas](../features/002-gestao-campanhas/spec.md) | RF02 | H03, H04 | Campanhas.Api | P1 |
| 003 | [Cadastro de doador](../features/003-cadastro-doador/spec.md) | RF03 | H01 | Identity.Api | P1 |
| 004 | [Painel de transparência](../features/004-painel-transparencia/spec.md) | RF04 | H05 | Campanhas.Api | P1 |
| 005 | [Doação assíncrona](../features/005-doacao/spec.md) | RF05 | H06, H07 | Campanhas.Api + Arrecadacao.Worker | P1 |
| 006 | [Busca de campanhas](../features/006-busca-campanhas/spec.md) | RF06 (Fase 4) | H08 | Campanhas.Api | P2 |
| 007 | [Recibo de doação](../features/007-recibo-doacao/spec.md) | RF07 (Fase 3) | H09 | Notificacoes.Function | P2 |
| 008 | [Direitos do titular](../features/008-direitos-titular/spec.md) | RF08 (Fase 5) | H10 | Identity.Api | P2 |
| 009 | [Testes unitários e cobertura no CI](../features/009-ci-testes-cobertura/spec.md) | Trabalho substitutivo | H11, H12 | Testes + GitHub Actions | **P0** |

Ordem de implementação sugerida: **009** (pipeline e base de testes, na Etapa 3) → 001 → 002 → 003 → 004 → 005 → 006 → 007 → 008. A 009 cresce a cada feature, que acrescenta seus testes (cada feature lista as dependências na própria spec).

Convenções comuns a todas as features:
- **400:** entrada mal formada (formato, campo obrigatório); **422:** entrada bem formada que viola regra de negócio.
- Todo erro usa `ProblemDetails` com `code` estável (ex.: `campanha.meta_invalida`).
- "Hoje" é a data atual em `America/Sao_Paulo`, obtida por `TimeProvider`.
- Cenários Gherkin de cada `spec.md` viram arquivos `.feature` em `tests/e2e/ConexaoSolidaria.Bdd/Features/`.

## Documentos do produto

| Documento | Conteúdo |
|---|---|
| [Requisitos não funcionais](requisitos-nao-funcionais.md) | Desempenho, escalabilidade, resiliência, segurança, LGPD, observabilidade e SLOs |
| [Domínio](dominio.md) | Subdomínios, bounded contexts, agregados, value objects e eventos |
| [Arquitetura](arquitetura.md) | Diagramas, componentes, fluxos, camadas e configuração |
| [API e eventos](api-e-eventos.md) | Convenções da API, endpoints técnicos e contratos de eventos |
| [Dados](dados.md) | Bancos, justificativa, esquemas, cache, índice de busca e backup |
| [Segurança e LGPD](seguranca-e-lgpd.md) | Autenticação, autorização, dados pessoais, segredos e OWASP |
| [Observabilidade](observabilidade.md) | Health checks, métricas, dashboard, alertas, traces e logs |
| [Infraestrutura](infraestrutura.md) | Docker, docker-compose, Kubernetes, Kong, KEDA e Azure opcional |
| [CI/CD](ci-cd.md) | GitHub Actions, GHCR, proteção da `main` e releases |

A linguagem ubíqua está no [glossário](../../docs/glossary.md).
