# Plano de execução

[← README](../README.md)

Do ambiente pronto até a entrega. Cada etapa traz: **objetivo**, **tarefas**, **commits** (na ordem, um por arquivo ou pelo menor conjunto que compila — [granularidade](git-e-commits.md#granularidade)), **pronto quando** e **apoio** (skills, MCP, wiki).

Toda etapa termina com: `/code-review` → `git rebase --exec` (todo commit compila) → PR → CI verde → merge na `main` → item "Concluído" no Project.

As funcionalidades (Etapas 4 a 8) seguem o fluxo de Spec-Driven Development: as specs e planos já estão em [`specs/features/`](../specs/features/); cada feature é implementada com `/analyze` → `/implement`, e os commits são os do seu `tasks.md` ([Claude Code no projeto](claude-code.md)).

| Etapa | Nome | Sprint |
|---|---|---|
| 0 | Pré-requisitos | 0 |
| 1 | Fundação do repositório | 0 |
| 2 | Planejamento ágil e DDD | 0 |
| 3 | Esqueleto, testes e pipeline com cobertura (feature 009, avaliada) | 0 |
| 4 | Features de identidade (001, 003, 008) | 1 |
| 5 | Features de campanhas (002, 004, 006) | 1 |
| 6 | Feature de doação (005) | 2 |
| 7 | Feature de recibo (007) | 2 |
| 8 | Frontend: layout, navegação e IA (bônus) | 2 |
| 9 | Testes complementares | 2 |
| 10 | Containers e docker-compose | 3 |
| 11 | Kubernetes local | 3 |
| 12 | API Gateway (Kong) | 3 |
| 13 | Observabilidade | 3 |
| 14 | CI/CD complementar | 3 |
| 15 | Cloud opcional (Azure) | 4 |
| 16 | Revisão de segurança e LGPD | 4 |
| 17 | Documentação final | 4 |
| 18 | Vídeo | 4 |
| 19 | Conclusão | 4 |

---

## Etapa 0 — Pré-requisitos
**Objetivo:** máquina e contas prontas.
**Tarefas:** seguir [Pré-requisitos](pre-requisitos.md) integralmente.
**Commits:** nenhum.
**Pronto quando:** todos os comandos de verificação respondem sem erro e o Minikube sobe com o nó `Ready`.

## Etapa 1 — Fundação do repositório
**Objetivo:** repositório com regras, especificação e configuração do Claude Code.
**Tarefas:**
1. ✅ `git init`, branch `main`.
2. ✅ `.gitignore`, `README.md`, `CLAUDE.md`, `.mcp.json`, `.claude/` (settings, comandos, agentes, skills), `.vscode/`.
3. ✅ `specs/` (constituição, produto, 8 features), `docs/` (ADRs, runbooks, glossário, guias, manuais, roteiro do vídeo), guias de `tests/` e `scripts/`.
4. ✅ Repositório criado e enviado: `https://github.com/Kulessa/Trabalho` (o enunciado não exige que seja público).
5. Adicionar os integrantes como colaboradores.
6. Ligar secret scanning e push protection (Settings → Code security).

**Commits:** já feitos (um por arquivo).
**Pronto quando:** repositório no GitHub com a spec visível para o grupo.
**Apoio:** MCP GitHub ou `gh`.

## Etapa 2 — Planejamento ágil e DDD
**Objetivo:** backlog ligado às features, domínio modelado e specs validadas pelo grupo.
**Tarefas:**
1. GitHub Project com colunas, campos e labels do [processo ágil](processo-agil.md).
2. Uma issue por feature (H01–H12 agrupadas como em [visão geral](../specs/product/overview.md#catálogo-de-features)) e por tarefa técnica (T01–T04), com link para a `spec.md`; estimar com Planning Poker.
3. Revisar cada spec em grupo; ajustes com `/clarify` e `/analyze`.
4. `docs/agil/definition-of-ready.md` e `docs/agil/definition-of-done.md`.
5. Event Storming no Miro (fluxos "criar campanha" e "doar"); exportar PNGs.
6. Domain Storytelling da doação; exportar PNG.
7. `docs/ddd/context-map.md` (o glossário e os ADRs 0001–0017 já existem).
8. `.github/ISSUE_TEMPLATE/historia.md` e `.github/pull_request_template.md`.

**Commits (exemplos):**
```text
📝 docs(agil): adiciona definition of ready
📝 docs(agil): adiciona definition of done
📝 docs(ddd): adiciona event storming do fluxo de doação
📝 docs(ddd): adiciona event storming do fluxo de campanha
📝 docs(ddd): adiciona domain storytelling da doação
📝 docs(ddd): adiciona mapa de contextos
📝 docs: adiciona modelo de issue de história
📝 docs: adiciona modelo de pull request
```
**Pronto quando:** feature 009 pronta para a Etapa 3 e Sprint 1 planejada com as features 001, 002 e 003 na coluna "Pronto (DoR)" e `/analyze` sem achados críticos.
**Apoio:** `/clarify`, `/analyze`, MCP GitHub; `wiki:disciplinas/ddd`, `wiki:disciplinas/metodologias-ageis`.

## Etapa 3 — Esqueleto da solução e qualidade automática
**Objetivo:** solução compilando, hooks e a **pipeline de CI executando os testes unitários e retornando a cobertura** — o que o trabalho substitutivo avalia. Inclui as tarefas da [feature 009](../specs/features/009-ci-testes-cobertura/tasks.md).
**Tarefas e commits (nesta ordem; cada commit compila):**
```text
🔧 chore: adiciona global.json com SDK 10
🔧 chore: adiciona .editorconfig
🔧 chore: adiciona .gitattributes
🔧 chore: adiciona Directory.Build.props com avisos como erro
🔧 chore: adiciona Directory.Packages.props com gestão central de pacotes
🔧 chore: adiciona stylecop para exigir documentação xml
🎉 chore: cria solução ConexaoSolidaria
🎉 chore(contracts): cria projeto ConexaoSolidaria.Contracts
🎉 chore(defaults): cria projeto ConexaoSolidaria.ServiceDefaults
🎉 chore(identity): cria projetos Domain, Application, Infrastructure e Api
🎉 chore(campanhas): cria projetos Domain, Application, Infrastructure, Api e Worker
🎉 chore(notificacoes): cria projeto Notificacoes.Function
🎉 chore(web): cria projeto ConexaoSolidaria.Web
✅ test: cria projetos de testes unitários com xunit
✅ test: cria projetos de testes de integração, contrato e e2e
✅ test: adiciona teste de arquitetura das camadas
🔊 feat(defaults): adiciona logs estruturados com serilog
📈 feat(defaults): adiciona tracing com opentelemetry e endpoint de métricas
🩺 feat(defaults): adiciona health checks live e ready
🥅 feat(defaults): adiciona tratamento global de erros com problem details
🔧 chore: adiciona manifesto de ferramentas dotnet
🔧 chore: adiciona reportgenerator ao manifesto de ferramentas
🔧 chore: adiciona hooks do husky para build, formato e mensagem
🔧 chore: adiciona script de validação da mensagem de commit
🔧 chore: adiciona configuração de cobertura com coverlet
🔧 chore: adiciona script de verificação da cobertura mínima de 90%
👷 ci: adiciona pipeline com testes unitários e cobertura
👷 ci: adiciona testes de integração e cobertura mínima na pipeline
📝 docs: adiciona status da pipeline e cobertura no readme
📝 docs: atualiza CLAUDE.md com os comandos reais da solução
🔧 chore: adiciona script de verificação de pré-requisitos
```
Cada projeto novo entra na `.slnx` no mesmo commit em que é criado (`dotnet sln add`).
Os commits `feat(defaults)` vêm depois dos projetos de teste e cada um inclui os testes de sucesso e de erro do que adiciona, para a cobertura já nascer acima de 90%.
**Pronto quando:** `dotnet build -warnaserror` e `dotnet test` passam local e no CI; commit com mensagem fora do padrão é bloqueado; push e CI com cobertura abaixo de 90% falham; cada execução da pipeline mostra a cobertura no resumo e oferece o artefato `cobertura-testes-unitarios` para download.
**Apoio:** `/init` (revisar o `CLAUDE.md` existente), `/fewer-permission-prompts`, skills `commit-gitmoji` e `documentar-xml`, MCP Microsoft Learn.

## Etapas 4 a 8 — Features

Cada feature é uma branch `feat/NNN-nome`, um PR e uma sequência de commits definida no seu `tasks.md`.

**Ciclo por feature:**
1. `git switch -c feat/NNN-nome` a partir da `main` atualizada.
2. `/analyze NNN` → corrigir achados críticos na spec, no plano ou nas tarefas.
3. `/implement NNN` → tarefas em ordem, com TDD, um commit por tarefa.
4. Skill `verificar-cobertura` (≥ 90% por projeto) e agente `reviewer`.
5. `/code-review`; `/security-review` nas features com autenticação ou dados pessoais.
6. `git rebase --exec ...` → PR → CI verde → merge.

| Etapa | Features | Pronto quando |
|---|---|---|
| 4 — Identidade | [001 Autenticação](../specs/features/001-autenticacao/tasks.md), [003 Cadastro de doador](../specs/features/003-cadastro-doador/tasks.md), [008 Direitos do titular](../specs/features/008-direitos-titular/tasks.md) | Cenários de RF01, RF03 e RF08 passando; `/security-review` sem pendências críticas |
| 5 — Campanhas | [002 Gestão de campanhas](../specs/features/002-gestao-campanhas/tasks.md), [004 Painel](../specs/features/004-painel-transparencia/tasks.md), [006 Busca](../specs/features/006-busca-campanhas/tasks.md) | Cenários de RF02, RF04 e RF06 passando; evento no Outbox a cada alteração |
| 6 — Doação | [005 Doação assíncrona](../specs/features/005-doacao/tasks.md) | Doação aparece no painel em até 5 s; evento duplicado não soma duas vezes |
| 7 — Recibo | [007 Recibo de doação](../specs/features/007-recibo-doacao/tasks.md) | Cada doação gera um `ReciboEmitido` sem dados pessoais; spike registrado |
| 8 — Frontend | Layout, navegação e itens transversais do Blazor (as páginas de cada feature já vêm nas tarefas da feature) | Fluxo completo pelo navegador; testes bUnit passando |

**Commits da Etapa 8 (exemplos):**
```text
💄 feat(web): adiciona layout e navegação
💄 feat(web): adiciona página inicial
🤖 feat(web): adiciona sugestão de descrição com semantic kernel
✅ test(web): adiciona testes de layout e navegação com bunit
```
O assistente de IA (bônus) recebe só título e tópicos digitados pelo gestor e mostra o aviso de texto gerado por IA ([segurança e LGPD](../specs/product/seguranca-e-lgpd.md)).

**Apoio:** agentes `test-writer`, `implementer` e `reviewer`; skills `commit-gitmoji`, `documentar-xml`, `verificar-cobertura`, `nova-migration`; MCP Microsoft Learn e Playwright; `wiki:disciplinas/api-dotnet`, `wiki:disciplinas/mensageria`, `wiki:disciplinas/elasticsearch`, `wiki:disciplinas/serverless-apps`, `wiki:disciplinas/blazor-razor`.

## Etapa 9 — Testes complementares
**Objetivo:** cobertura de aceitação completa e metas de desempenho verificadas.
**Tarefas:** conferir que todo cenário das specs está automatizado; E2E Playwright da jornada "doar" (bônus); bônus GraphQL/gRPC se houver tempo; rodar os três scripts k6 depois da Etapa 11.
**Commits (exemplos):**
```text
✅ test(e2e): adiciona jornada de doação com playwright
✅ test(load): ajusta thresholds do k6 após medição no minikube
```
**Pronto quando:** todos os RFs com cenário automatizado; thresholds do k6 atendidos; cobertura ≥ 90% em todos os projetos.
**Apoio:** MCP Playwright; `wiki:disciplinas/qualidade-software`.

## Etapa 10 — Containers e docker-compose
**Objetivo:** stack completa com um comando.
**Tarefas:** `.dockerignore`; Dockerfile por serviço (+ estágio `migrator`); `deploy/docker-compose.yml`; `deploy/.env.example`; `scripts/seed-demo.ps1`.
**Commits (exemplos):**
```text
🙈 chore: adiciona .dockerignore
🧱 build(identity): adiciona dockerfile multi-stage da identity-api
🧱 build(campanhas): adiciona dockerfile multi-stage da campanhas-api
🧱 build(worker): adiciona dockerfile multi-stage do worker
🧱 build(web): adiciona dockerfile multi-stage do web
🧱 build: adiciona docker-compose com toda a stack
🔐 chore: adiciona modelo de variáveis de ambiente
🌱 chore: adiciona script de dados de demonstração
```
**Pronto quando:** `docker compose up -d` + seed → doação de ponta a ponta funcionando.
**Apoio:** `/run`, `wiki:disciplinas/docker`.

## Etapa 11 — Kubernetes local
**Objetivo:** tudo rodando no Minikube com os manifestos `.yaml` (conteúdo da Fase 2).
**Tarefas:** estrutura Kustomize; namespaces; infra (StatefulSets, PVC, Services); apps (Deployments, Services, ConfigMaps, HPA, PDB, Jobs de migration, NetworkPolicies, ServiceAccounts); modelos de Secret; `scripts/k8s-secrets.ps1`; `scripts/up.ps1`.
**Commits (exemplos):**
```text
🧱 build(k8s): adiciona namespaces
🧱 build(k8s): adiciona statefulset do postgres
🧱 build(k8s): adiciona statefulset do mongodb
🧱 build(k8s): adiciona deployment do redis
🧱 build(k8s): adiciona statefulset do rabbitmq com definições
🧱 build(k8s): adiciona statefulset do elasticsearch
🧱 build(k8s): adiciona jobs de migration
🧱 build(k8s): adiciona deployment, service e configmap da identity-api
🧱 build(k8s): adiciona deployment, service e configmap da campanhas-api
🧱 build(k8s): adiciona deployment do worker
🧱 build(k8s): adiciona deployment da função de notificações
🧱 build(k8s): adiciona deployment do web
🧱 build(k8s): adiciona hpa e pdb das apis
🔒️ build(k8s): adiciona network policies
🔐 chore(k8s): adiciona modelos de secrets
🔧 chore: adiciona script de criação de secrets
🔧 chore: adiciona script de subida do ambiente
🧱 build(k8s): adiciona overlay local
```
Validação de cada manifesto antes do commit: `kubectl apply -k deploy/k8s/overlays/local --dry-run=server`.
**Pronto quando:** `kubectl get pods -A` com tudo `Running`/`Completed`; doação de ponta a ponta no cluster.
**Apoio:** `/run`, `wiki:disciplinas/kubernetes`.

## Etapa 12 — API Gateway (Kong)
**Objetivo:** entrada única com TLS, rate limiting e métricas.
**Tarefas:** instalar o Kong Ingress Controller; certificado mkcert; `Ingress` por serviço; `KongPlugin`/`KongClusterPlugin`; atualizar o Web e a coleção Postman para usar o gateway.
**Commits (exemplos):**
```text
🧱 build(kong): adiciona ingress das apis
🧱 build(kong): adiciona ingress do web com afinidade de sessão
🔒️ build(kong): adiciona rate limiting, cors e limite de corpo
📈 build(kong): adiciona plugins de correlation-id e prometheus
```
**Pronto quando:** todas as chamadas passam pelo Kong; 429 ao exceder o limite do login.
**Apoio:** `wiki:disciplinas/api-gateway`.

## Etapa 13 — Observabilidade
**Objetivo:** dashboard Grafana com métricas reais, alertas e traces.
**Tarefas:** instalar kube-prometheus-stack e Jaeger; `ServiceMonitor` por serviço, RabbitMQ e Kong; dashboard JSON; `PrometheusRule`; runbooks; Zabbix opcional; KEDA (`ScaledObject` do Worker e da função).
**Commits (exemplos):**
```text
📈 build(obs): adiciona service monitors das aplicações
📈 build(obs): adiciona coleta de métricas do rabbitmq e do kong
📈 feat(obs): adiciona dashboard conexão solidária
📈 feat(obs): adiciona regras de alerta
📝 docs(obs): adiciona runbooks dos alertas
🧱 build(k8s): adiciona scaledobjects do keda para worker e função
```
**Pronto quando:** dashboard mostra dados reais durante o k6; trace da doação completo no Jaeger; função escala de 0 a 1 e volta.
**Apoio:** `dataviz` (opcional), `wiki:disciplinas/monitoramento-acesso`.

## Etapa 14 — CI/CD
**Objetivo:** completar o CI além do que é avaliado (os testes e a cobertura já existem desde a Etapa 3) e proteger a `main`.
**Tarefas:** completar `ci.yml` (gitleaks, matriz Docker, Trivy, push GHCR); `codeql.yml`; `dependabot.yml`; proteção da `main`; `cd.yml` opcional.
**Commits (exemplos):**
```text
👷 ci: adiciona varredura de segredos com gitleaks
👷 ci: adiciona build e push das imagens no ghcr
👷 ci: adiciona varredura de vulnerabilidades com trivy
👷 ci: adiciona análise com codeql
🔧 chore: adiciona configuração do dependabot
```
**Pronto quando:** push na `main` gera imagens `sha-<commit>` no GHCR; PR não faz merge com CI vermelho.
**Apoio:** `/loop`, MCP GitHub, `wiki:disciplinas/github-actions`.

## Etapa 15 — Cloud opcional (Azure)
**Objetivo:** demonstrar a mesma arquitetura no AKS (se o grupo decidir).
**Tarefas:** alerta de orçamento; `deploy/azure/main.bicep`; overlay `azure`; federação OIDC GitHub ↔ Azure; `cd.yml`; deploy com Rolling Update; **apagar tudo após a gravação**.
**Commits (exemplos):**
```text
🧱 build(azure): adiciona bicep com acr, aks e key vault
🧱 build(k8s): adiciona overlay azure
👷 ci: adiciona deploy no aks com oidc e rolling update
```
**Pronto quando:** aplicação acessível no AKS; `az group delete` executado após o vídeo.
**Apoio:** `/security-review`, `wiki:disciplinas/orquestracao-k8s`.

## Etapa 16 — Revisão de segurança e LGPD
**Objetivo:** nenhuma pendência crítica.
**Tarefas:** `/security-review` completo; checklist OWASP de [segurança e LGPD](../specs/product/seguranca-e-lgpd.md#checklist-owasp-top-10-revisão-na-etapa-16); registro de tratamento; política de privacidade; conferir logs reais em busca de dados pessoais.
**Commits (exemplos):**
```text
📝 docs(lgpd): adiciona registro das operações de tratamento
📝 docs(lgpd): adiciona política de privacidade
🔒️ fix(...): <correções apontadas na revisão>
```
**Pronto quando:** checklist OWASP e LGPD 100%; gitleaks e Trivy verdes.

## Etapa 17 — Documentação final
**Objetivo:** repositório que qualquer pessoa consegue rodar e entender.
**Tarefas:**
1. README final: visão, arquitetura, pré-requisitos, como rodar os testes e ver a cobertura, passo a passo com docker-compose e com Kubernetes, URLs, usuários de teste, variáveis.
2. `docs/arquitetura/arquitetura.png` (exportado do Mermaid).
3. `docs/justificativa-bancos.md` (conteúdo de persistência poliglota da Fase 4).
4. Coleção e ambiente Postman.

**Commits (exemplos):**
```text
📝 docs: adiciona diagrama de arquitetura em png
📝 docs: adiciona justificativa dos bancos de dados
📝 docs: adiciona coleção postman
📝 docs: atualiza README com passo a passo completo
```
**Pronto quando:** outra pessoa roda os testes e sobe a API do zero seguindo só o README.

## Etapa 18 — Vídeo
**Objetivo:** vídeo que demonstra todos os passos do enunciado: API, testes unitários de sucesso e erro, pipeline executando e cobertura com download.
**Tarefas:** seguir o [roteiro do vídeo](ROTEIRO-VIDEO.md): preparação, ensaio, gravação, revisão, publicação.
**Commits:** `📝 docs: adiciona link do vídeo no README`.
**Pronto quando:** vídeo publicado, link testado em aba anônima e todos os itens do [checklist do enunciado](entrega-e-riscos.md#checklist-do-enunciado-com-evidência) aparecendo no vídeo.

## Etapa 19 — Conclusão
**Objetivo:** entrega final.
**Tarefas:**
1. Checklist de [entrega e riscos](entrega-e-riscos.md) 100%.
2. `git tag -a v1.0.0` + push; `gh release create v1.0.0 --generate-notes`.
3. Confirmar o repositório acessível e o README funcionando.
4. Apagar recursos de cloud, se houver.
5. Enviar o link do vídeo na plataforma da FIAP.
6. Retrospectiva final do projeto.

**Commits:** `🔖 chore: versão 1.0.0` (atualiza versão nos `Directory.Build.props`).
**Pronto quando:** entrega registrada na plataforma.
