# Processo ágil

[← README](../README.md)

Como o grupo organiza o trabalho. Aplica Scrum, Kanban, XP e Lean da Fase 5 (`wiki:disciplinas/metodologias-ageis`).

## Framework

**Scrum** com sprints de 1 semana para cadência e metas, e **Kanban** no quadro para controlar o fluxo (WIP). Práticas de engenharia do **XP**: TDD, integração contínua, programação em par, refatoração e design simples.

## Papéis

| Papel | Responsabilidade no projeto | Integrante |
|---|---|---|
| Product Owner | Ordena o backlog a partir do enunciado; aceita as histórias | _a definir_ |
| Scrum Master | Facilita as cerimônias, remove impedimentos, cuida do quadro | _a definir_ |
| Developers | Implementam, testam e documentam | Todos |

Em grupo pequeno, PO e Scrum Master acumulam papel de Developer.

## Sprints

Ajustar as datas ao prazo de entrega da FIAP. A sprint só termina com o incremento cumprindo a [Definition of Done](#definition-of-done-dod).

| Sprint | Meta da sprint | Etapas do plano |
|---|---|---|
| 0 | Ambiente pronto, domínio modelado e solução compilando no CI | 0, 1, 2, 3 |
| 1 | Cadastro, login e gestão de campanhas funcionando | 4, 5 |
| 2 | Doação assíncrona de ponta a ponta, recibo, frontend e testes | 6, 7, 8, 9 |
| 3 | Tudo rodando no Kubernetes com gateway, observabilidade e CI/CD | 10, 11, 12, 13, 14 |
| 4 | Revisões, documentação, vídeo e entrega | 15, 16, 17, 18, 19 |

## Cerimônias (sprint de 1 semana)

| Cerimônia | Quando | Time-box | Saída |
|---|---|---|---|
| Sprint Planning | Início da sprint | 2 h | Meta da sprint e itens selecionados no quadro |
| Daily | Todo dia, no Discord | 15 min | Plano do dia e impedimentos |
| Refinamento | Meio da sprint | 1 h | Histórias da próxima sprint cumprindo a DoR |
| Sprint Review | Fim da sprint | 1 h | Demonstração do incremento rodando |
| Retrospectiva | Após a Review | 45 min | 1 a 3 ações com responsável, revisadas na sprint seguinte |

## Quadro Kanban (GitHub Projects)

| Coluna | Entra quando | Limite de WIP |
|---|---|---|
| Backlog | Item criado | — |
| Pronto (DoR) | Cumpre a Definition of Ready | — |
| Em andamento | Alguém começou | 3 |
| Revisão | PR aberto com CI verde | 3 |
| Concluído | Merge na `main` e DoD cumprida | — |

**Campos do Project:** `Sprint` (iteração), `Estimativa` (pontos), `Etapa` (0–19), `Tipo` (história, tarefa técnica, bug, spike).

**Labels:** `feature-001`…`feature-008`, `rf01`…`rf08`, `infra`, `observabilidade`, `seguranca`, `lgpd`, `docs`, `bonus`, `bug`.

**Rastreabilidade:** cada PR cita a issue (`Refs #12` ou `Closes #12`) e cada commit relevante cita a issue no rodapé.

## Definition of Ready (DoR)

Um item entra em "Pronto" quando:

- [ ] Está escrito como história (`Como <persona>, quero <ação>, para <benefício>`) ou tarefa técnica com objetivo claro.
- [ ] A feature tem `spec.md` em `specs/features/NNN-nome/` sem `[PRECISA ESCLARECER]` (`/specify` e `/clarify`).
- [ ] Tem critérios de aceitação testáveis em Gherkin na spec.
- [ ] `plan.md` e `tasks.md` existem e o `/analyze` não aponta achados críticos.
- [ ] Atende ao INVEST (independente, negociável, valiosa, estimável, pequena, testável).
- [ ] Está estimado e cabe em até 3 dias de trabalho.
- [ ] Dependências identificadas.

## Definition of Done (DoD)

Um item só vai para "Concluído" quando:

- [ ] Código segue [Padrões de código](padroes-de-codigo.md), com `/// <summary>` em todos os tipos e membros.
- [ ] Testes de unidade escritos (TDD no domínio) e cenários Gherkin da história automatizados.
- [ ] Cobertura de testes ≥ 90% de linhas e ramos, no total e em cada projeto de `src/`.
- [ ] Todos os commits no padrão e compilando ([Git e commits](git-e-commits.md)).
- [ ] CI verde: formato, build, testes, scans.
- [ ] `/code-review` executado e apontamentos tratados.
- [ ] Revisão de pelo menos um integrante no PR.
- [ ] Logs, métricas e health checks do que foi criado.
- [ ] Nenhum dado pessoal em logs ou eventos.
- [ ] Todas as tarefas do `tasks.md` marcadas `[X]` e `Status` da spec como `Implementada`.
- [ ] Documentação atualizada (README, spec, ADR quando houver decisão).
- [ ] Demonstrado rodando localmente.

## Estimativa

- **Planning Poker** com Fibonacci (1, 2, 3, 5, 8, 13). Item de 13 pontos é dividido.
- **Spike** com time-box (máx. 1 dia) para incertezas técnicas. Candidatos: gatilho RabbitMQ no Azure Functions com envelope do MassTransit; recursos do Minikube com toda a stack.

## Histórias de usuário (backlog inicial)

| ID | História | RF | Feature | Pontos (sugestão) |
|---|---|---|---|---|
| H01 | Como visitante, quero me cadastrar como doador, para poder doar | RF03 | [003](../specs/features/003-cadastro-doador/spec.md) | 5 |
| H02 | Como doador ou gestor, quero entrar com e-mail e senha, para acessar minhas funções | RF01 | [001](../specs/features/001-autenticacao/spec.md) | 5 |
| H03 | Como gestor, quero criar campanhas com meta e período, para arrecadar | RF02 | [002](../specs/features/002-gestao-campanhas/spec.md) | 5 |
| H04 | Como gestor, quero editar, concluir e cancelar campanhas, para mantê-las corretas | RF02 | [002](../specs/features/002-gestao-campanhas/spec.md) | 3 |
| H05 | Como visitante, quero ver campanhas ativas e o total arrecadado, para confiar na ONG | RF04 | [004](../specs/features/004-painel-transparencia/spec.md) | 3 |
| H06 | Como doador, quero doar para uma campanha ativa, para ajudar a ONG | RF05 | [005](../specs/features/005-doacao/spec.md) | 8 |
| H07 | Como gestor, quero que o total seja atualizado automaticamente após cada doação, para acompanhar a meta | RF05 | [005](../specs/features/005-doacao/spec.md) | 8 |
| H08 | Como visitante, quero buscar campanhas mesmo com erro de digitação, para achá-las rápido | RF06 | [006](../specs/features/006-busca-campanhas/spec.md) | 5 |
| H09 | Como doador, quero receber um recibo da doação, para ter um comprovante | RF07 | [007](../specs/features/007-recibo-doacao/spec.md) | 5 |
| H10 | Como doador, quero ver e apagar meus dados, para exercer meus direitos da LGPD | RF08 | [008](../specs/features/008-direitos-titular/spec.md) | 5 |
| T01 | Containerizar e orquestrar no Kubernetes | — | Etapas 10–14 | 8 |
| T02 | Dashboard Grafana com métricas reais | — | Etapas 10–14 | 5 |
| T03 | Pipeline de CI com imagem Docker | — | Etapas 10–14 | 5 |
| T04 | API Gateway Kong | — | Etapas 10–14 | 5 |

## Métricas do time

| Métrica | Onde | Uso |
|---|---|---|
| Burndown da sprint | GitHub Projects (gráfico de insights) | Ajustar o plano durante a sprint |
| Lead time (issue aberta → merge) | GitHub Projects | Encontrar gargalos |
| Vazão (itens concluídos por sprint) | GitHub Projects | Planejar a próxima sprint |
| CI quebrado na `main` | GitHub Actions | Deve ser zero; corrigir com prioridade |

## Práticas XP

| Prática | Aplicação |
|---|---|
| TDD | Obrigatório nos agregados `Campanha` e `Usuario` e nos value objects |
| Programação em par | Outbox, consumidor idempotente e configuração do Kubernetes |
| Integração contínua | Branches de no máximo 2 dias; merge diário quando possível |
| Refatoração | Skill `/simplify` ao fim de cada etapa |
| Design simples | Sem abstrações sem uso (YAGNI) |
| Propriedade coletiva | Qualquer integrante pode alterar qualquer parte, com PR |
