# Visão geral do produto

[← README](../../README.md) · [Constituição](../constitution.md)

Ponto de partida da especificação: o problema, os objetivos, quem usa e o que está dentro e fora do escopo. Os detalhes de cada funcionalidade estão em [`specs/features/`](../features/); as decisões transversais, nos demais arquivos desta pasta.

## Contexto

A **ONG Esperança Solidária** acolhe crianças em situação de vulnerabilidade há mais de 10 anos. Hoje ela controla doadores e campanhas em planilhas e mensagens, o que causa:

- falta de transparência sobre quanto cada campanha arrecadou;
- retrabalho para cadastrar doadores e conferir doações;
- nenhuma visibilidade de uso, erros ou desempenho.

A plataforma **Conexão Solidária** é o MVP que resolve esses problemas. O enunciado pede foco em **escalabilidade, observabilidade e automação**, e a nota considera a **arquitetura**, não só o funcionamento.

Fonte: `wiki:fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria`.

## Objetivos

| # | Objetivo | Como é medido |
|---|---|---|
| O1 | Atender todos os requisitos obrigatórios do Hackathon | Checklist em [Entrega e riscos](../../docs/entrega-e-riscos.md) 100% marcado |
| O2 | Aplicar o conteúdo das 5 fases do curso | Matriz em [Cobertura das fases](../../docs/cobertura-das-fases.md) |
| O3 | Seguir as boas práticas da LLMWiki | Checklists de `specs/product/` e `docs/` marcados |
| O4 | Manter todo commit compilável e no padrão | Hooks locais e CI verdes ([Git e commits](../../docs/git-e-commits.md)) |
| O5 | Comprovar a arquitetura em até 15 minutos de vídeo | [Roteiro do vídeo](../../docs/ROTEIRO-VIDEO.md) |

## Personas

| Persona | Quem é | O que precisa |
|---|---|---|
| **GestorONG** | Funcionário da ONG que administra campanhas | Criar, editar, concluir e cancelar campanhas; ver doações |
| **Doador** | Pessoa física que contribui | Cadastrar-se, entrar, doar e ver os próprios dados |
| **Visitante** | Qualquer pessoa, sem login | Ver campanhas ativas, quanto foi arrecadado e buscar campanhas |
| **Operador da plataforma** | Equipe técnica (o grupo) | Implantar, monitorar e diagnosticar a plataforma |

## Escopo

### Dentro do escopo (obrigatório pelo enunciado)

1. Autenticação JWT com as roles `GestorONG` e `Doador`.
2. Gestão de campanhas restrita a `GestorONG`.
3. Cadastro público de doador.
4. Painel público de transparência com o total arrecadado.
5. Doação por doador autenticado, processada de forma **assíncrona** por um Worker.
6. No mínimo 2 microsserviços (entregues: 4 componentes de backend).
7. Mensageria: a API publica `DoacaoRecebidaEvent`; o Worker atualiza o total.
8. Kubernetes local com os `.yaml` (Deployments, Services, ConfigMaps).
9. `/health` e `/metrics`; dashboard Grafana com métricas reais.
10. CI a cada push na `main` com build .NET e imagem Docker.

### Dentro do escopo (bônus do enunciado)

11. Testes de unidade rodando no CI.
12. API Gateway roteando para os microsserviços (Kong).

### Dentro do escopo (para cobrir as 5 fases)

13. Busca fuzzy de campanhas (Elasticsearch) — Fase 4.
14. Recibo de doação por função serverless — Fase 3.
15. Livro de doações como audit log (MongoDB) — Fases 3 e 4.
16. Cache do painel (Redis) — Fases 1 e 4.
17. Frontend Blazor — Fase 5.
18. Direitos do titular (LGPD) — Fase 5.
19. Deploy opcional na Azure — Fases 2 e 4.
20. Assistente de IA opcional para descrição de campanhas — introdução da Fase 5.

### Fora do escopo

- Pagamento real (cartão, Pix). A doação é registrada como intenção já confirmada.
- Envio real de e-mail. O recibo é simulado em log.
- Recuperação de senha, refresh token e login social.
- Ambiente de produção permanente na cloud.
- Aplicativo mobile.

## Premissas

- Cada integrante usa Windows, macOS ou Linux, com 16 GB de RAM ou mais para rodar o cluster local ([Pré-requisitos](../../docs/pre-requisitos.md)).
- O repositório será **público** no GitHub.
- A doação é considerada paga no momento do envio (o enunciado não pede gateway de pagamento).
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

Ordem de implementação sugerida: 001 → 002 → 003 → 004 → 005 → 006 → 007 → 008 (cada feature lista as dependências na própria spec).

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
