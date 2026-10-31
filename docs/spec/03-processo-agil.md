# 03 — Processo ágil

[← README](../../README.md) · Anterior: [02 — Pré-requisitos](02-pre-requisitos.md) · Próximo: [04 — Requisitos funcionais](04-requisitos-funcionais.md)

Como o grupo organiza o trabalho. Aplica Scrum, Kanban, XP e Lean da Fase 5 (`wiki:disciplinas/metodologias-ageis`).

## 3.1 Framework

**Scrum** com sprints de 1 semana para cadência e metas, e **Kanban** no quadro para controlar o fluxo (WIP). Práticas de engenharia do **XP**: TDD, integração contínua, programação em par, refatoração e design simples.

## 3.2 Papéis

| Papel | Responsabilidade no projeto | Integrante |
|---|---|---|
| Product Owner | Ordena o backlog a partir do enunciado; aceita as histórias | _a definir_ |
| Scrum Master | Facilita as cerimônias, remove impedimentos, cuida do quadro | _a definir_ |
| Developers | Implementam, testam e documentam | Todos |

Em grupo pequeno, PO e Scrum Master acumulam papel de Developer.

## 3.3 Sprints

Ajustar as datas ao prazo de entrega da FIAP. A sprint só termina com o incremento cumprindo a [Definition of Done](#37-definition-of-done-dod).

| Sprint | Meta da sprint | Etapas do plano |
|---|---|---|
| 0 | Ambiente pronto, domínio modelado e solução compilando no CI | 0, 1, 2, 3 |
| 1 | Cadastro, login e gestão de campanhas funcionando | 4, 5 |
| 2 | Doação assíncrona de ponta a ponta, recibo, frontend e testes | 6, 7, 8, 9 |
| 3 | Tudo rodando no Kubernetes com gateway, observabilidade e CI/CD | 10, 11, 12, 13, 14 |
| 4 | Revisões, documentação, vídeo e entrega | 15, 16, 17, 18, 19 |

## 3.4 Cerimônias (sprint de 1 semana)

| Cerimônia | Quando | Time-box | Saída |
|---|---|---|---|
| Sprint Planning | Início da sprint | 2 h | Meta da sprint e itens selecionados no quadro |
| Daily | Todo dia, no Discord | 15 min | Plano do dia e impedimentos |
| Refinamento | Meio da sprint | 1 h | Histórias da próxima sprint cumprindo a DoR |
| Sprint Review | Fim da sprint | 1 h | Demonstração do incremento rodando |
| Retrospectiva | Após a Review | 45 min | 1 a 3 ações com responsável, revisadas na sprint seguinte |

## 3.5 Quadro Kanban (GitHub Projects)

| Coluna | Entra quando | Limite de WIP |
|---|---|---|
| Backlog | Item criado | — |
| Pronto (DoR) | Cumpre a Definition of Ready | — |
| Em andamento | Alguém começou | 3 |
| Revisão | PR aberto com CI verde | 3 |
| Concluído | Merge na `main` e DoD cumprida | — |

**Campos do Project:** `Sprint` (iteração), `Estimativa` (pontos), `Etapa` (0–19), `Tipo` (história, tarefa técnica, bug, spike).

**Labels:** `rf01`…`rf08`, `infra`, `observabilidade`, `seguranca`, `lgpd`, `docs`, `bonus`, `bug`.

**Rastreabilidade:** cada PR cita a issue (`Refs #12` ou `Closes #12`) e cada commit relevante cita a issue no rodapé.

## 3.6 Definition of Ready (DoR)

Um item entra em "Pronto" quando:

- [ ] Está escrito como história (`Como <persona>, quero <ação>, para <benefício>`) ou tarefa técnica com objetivo claro.
- [ ] Tem critérios de aceitação testáveis em Gherkin.
- [ ] Atende ao INVEST (independente, negociável, valiosa, estimável, pequena, testável).
- [ ] Está estimado e cabe em até 3 dias de trabalho.
- [ ] Dependências identificadas.

## 3.7 Definition of Done (DoD)

Um item só vai para "Concluído" quando:

- [ ] Código segue [16 — Padrões de código](16-padroes-de-codigo.md).
- [ ] Testes de unidade escritos (TDD no domínio) e cenários Gherkin da história automatizados.
- [ ] Todos os commits no padrão e compilando ([17 — Git e commits](17-git-e-commits.md)).
- [ ] CI verde: formato, build, testes, scans.
- [ ] `/code-review` executado e apontamentos tratados.
- [ ] Revisão de pelo menos um integrante no PR.
- [ ] Logs, métricas e health checks do que foi criado.
- [ ] Nenhum dado pessoal em logs ou eventos.
- [ ] Documentação atualizada (README, spec, ADR quando houver decisão).
- [ ] Demonstrado rodando localmente.

## 3.8 Estimativa

- **Planning Poker** com Fibonacci (1, 2, 3, 5, 8, 13). Item de 13 pontos é dividido.
- **Spike** com time-box (máx. 1 dia) para incertezas técnicas. Candidatos: gatilho RabbitMQ no Azure Functions com envelope do MassTransit; recursos do Minikube com toda a stack.

## 3.9 Histórias de usuário (backlog inicial)

| ID | História | RF | Pontos (sugestão) |
|---|---|---|---|
| H01 | Como visitante, quero me cadastrar como doador, para poder doar | RF03 | 5 |
| H02 | Como doador ou gestor, quero entrar com e-mail e senha, para acessar minhas funções | RF01 | 5 |
| H03 | Como gestor, quero criar campanhas com meta e período, para arrecadar | RF02 | 5 |
| H04 | Como gestor, quero editar, concluir e cancelar campanhas, para mantê-las corretas | RF02 | 3 |
| H05 | Como visitante, quero ver campanhas ativas e o total arrecadado, para confiar na ONG | RF04 | 3 |
| H06 | Como doador, quero doar para uma campanha ativa, para ajudar a ONG | RF05 | 8 |
| H07 | Como gestor, quero que o total seja atualizado automaticamente após cada doação, para acompanhar a meta | RF05 | 8 |
| H08 | Como visitante, quero buscar campanhas mesmo com erro de digitação, para achá-las rápido | RF06 | 5 |
| H09 | Como doador, quero receber um recibo da doação, para ter um comprovante | RF07 | 5 |
| H10 | Como doador, quero ver e apagar meus dados, para exercer meus direitos da LGPD | RF08 | 5 |
| T01 | Containerizar e orquestrar no Kubernetes | — | 8 |
| T02 | Dashboard Grafana com métricas reais | — | 5 |
| T03 | Pipeline de CI com imagem Docker | — | 5 |
| T04 | API Gateway Kong | — | 5 |

## 3.10 Métricas do time

| Métrica | Onde | Uso |
|---|---|---|
| Burndown da sprint | GitHub Projects (gráfico de insights) | Ajustar o plano durante a sprint |
| Lead time (issue aberta → merge) | GitHub Projects | Encontrar gargalos |
| Vazão (itens concluídos por sprint) | GitHub Projects | Planejar a próxima sprint |
| CI quebrado na `main` | GitHub Actions | Deve ser zero; corrigir com prioridade |

## 3.11 Práticas XP

| Prática | Aplicação |
|---|---|
| TDD | Obrigatório nos agregados `Campanha` e `Usuario` e nos value objects |
| Programação em par | Outbox, consumidor idempotente e configuração do Kubernetes |
| Integração contínua | Branches de no máximo 2 dias; merge diário quando possível |
| Refatoração | Skill `/simplify` ao fim de cada etapa |
| Design simples | Sem abstrações sem uso (YAGNI) |
| Propriedade coletiva | Qualquer integrante pode alterar qualquer parte, com PR |
