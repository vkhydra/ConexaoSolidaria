# 01 — Visão geral e escopo

[← README](../../README.md) · Próximo: [02 — Pré-requisitos](../../docs/pre-requisitos.md)

## 1.1 Contexto

A **ONG Esperança Solidária** acolhe crianças em situação de vulnerabilidade há mais de 10 anos. Hoje ela controla doadores e campanhas em planilhas e mensagens, o que causa:

- falta de transparência sobre quanto cada campanha arrecadou;
- retrabalho para cadastrar doadores e conferir doações;
- nenhuma visibilidade de uso, erros ou desempenho.

A plataforma **Conexão Solidária** é o MVP que resolve esses problemas. O enunciado pede foco em **escalabilidade, observabilidade e automação**, e a nota considera a **arquitetura**, não só o funcionamento.

Fonte: `wiki:fontes/tech-challenge/05-hackathon-fase-5-conexao-solidaria`.

## 1.2 Objetivos

| # | Objetivo | Como é medido |
|---|---|---|
| O1 | Atender todos os requisitos obrigatórios do Hackathon | Checklist em [21 — Entrega e riscos](../../docs/entrega-e-riscos.md) 100% marcado |
| O2 | Aplicar o conteúdo das 5 fases do curso | Matriz em [18 — Cobertura das fases](../../docs/cobertura-das-fases.md) |
| O3 | Seguir as boas práticas da LLMWiki | Checklists das seções 10 a 17 marcados |
| O4 | Manter todo commit compilável e no padrão | Hooks locais e CI verdes ([17 — Git e commits](../../docs/git-e-commits.md)) |
| O5 | Comprovar a arquitetura em até 15 minutos de vídeo | [Roteiro do vídeo](../../docs/ROTEIRO-VIDEO.md) |

## 1.3 Personas

| Persona | Quem é | O que precisa |
|---|---|---|
| **GestorONG** | Funcionário da ONG que administra campanhas | Criar, editar, concluir e cancelar campanhas; ver doações |
| **Doador** | Pessoa física que contribui | Cadastrar-se, entrar, doar e ver os próprios dados |
| **Visitante** | Qualquer pessoa, sem login | Ver campanhas ativas, quanto foi arrecadado e buscar campanhas |
| **Operador da plataforma** | Equipe técnica (o grupo) | Implantar, monitorar e diagnosticar a plataforma |

## 1.4 Escopo

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

## 1.5 Premissas

- Cada integrante usa Windows, macOS ou Linux, com 16 GB de RAM ou mais para rodar o cluster local ([02 — Pré-requisitos](../../docs/pre-requisitos.md)).
- O repositório será **público** no GitHub.
- A doação é considerada paga no momento do envio (o enunciado não pede gateway de pagamento).
- O fuso horário de referência para regras de data é `America/Sao_Paulo`.
- Valores monetários em reais (BRL), com 2 casas decimais.

## 1.6 Linguagem ubíqua (glossário)

Os termos abaixo são usados igualmente no código, na API, nos testes e na documentação (`wiki:conceitos/linguagem-ubiqua`).

| Termo | Definição | No código |
|---|---|---|
| Campanha | Ação de arrecadação com meta e período | `Campanha` |
| Meta financeira | Valor que a campanha pretende arrecadar | `MetaFinanceira` |
| Total arrecadado | Soma das doações **processadas** da campanha | `TotalArrecadado` |
| Campanha ativa | Campanha com status `Ativa` | `StatusCampanha.Ativa` |
| Campanha concluída | Encerrada pelo gestor após o período ou meta | `StatusCampanha.Concluida` |
| Campanha cancelada | Encerrada pelo gestor antes do fim, sem conclusão | `StatusCampanha.Cancelada` |
| Doador | Pessoa cadastrada que faz doações | `Usuario` com `Role.Doador` |
| Gestor da ONG | Usuário que administra campanhas | `Usuario` com `Role.GestorONG` |
| Doação recebida | Doação aceita pela API e ainda não somada ao total | `DoacaoRecebidaEvent` |
| Doação processada | Doação já somada ao total pelo Worker | `DoacaoProcessadaEvent` |
| Livro de doações | Registro imutável de todas as doações processadas | coleção `lancamentos_doacao` |
| Painel de transparência | Visão pública das campanhas ativas e do total | `GET /transparencia/campanhas` |
| Recibo | Comprovante simulado enviado ao doador | `Notificacoes.Function` |
| Anonimização | Remoção irreversível dos dados pessoais do doador | `Usuario.Anonimizar()` |
