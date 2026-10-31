# 20 — Plano de execução

[← README](../../README.md) · Anterior: [19 — Skills e MCP](19-skills-e-mcp.md) · Próximo: [21 — Entrega e riscos](21-entrega-e-riscos.md)

Do ambiente pronto até a entrega. Cada etapa traz: **objetivo**, **tarefas**, **commits** (na ordem, um por arquivo ou pelo menor conjunto que compila — [seção 17.3](17-git-e-commits.md#173-granularidade)), **pronto quando** e **apoio** (skills, MCP, wiki).

Toda etapa termina com: `/code-review` → `git rebase --exec` (todo commit compila) → PR → CI verde → merge na `main` → item "Concluído" no Project.

| Etapa | Nome | Sprint |
|---|---|---|
| 0 | Pré-requisitos | 0 |
| 1 | Fundação do repositório | 0 |
| 2 | Planejamento ágil e DDD | 0 |
| 3 | Esqueleto da solução e qualidade automática | 0 |
| 4 | Identity.Api | 1 |
| 5 | Campanhas.Api | 1 |
| 6 | Arrecadacao.Worker e mensageria | 2 |
| 7 | Notificacoes.Function | 2 |
| 8 | Frontend Blazor (+ IA bônus) | 2 |
| 9 | Testes complementares | 2 |
| 10 | Containers e docker-compose | 3 |
| 11 | Kubernetes local | 3 |
| 12 | API Gateway (Kong) | 3 |
| 13 | Observabilidade | 3 |
| 14 | CI/CD | 3 |
| 15 | Cloud opcional (Azure) | 4 |
| 16 | Revisão de segurança e LGPD | 4 |
| 17 | Documentação e entregáveis | 4 |
| 18 | Vídeo | 4 |
| 19 | Conclusão | 4 |

---

## Etapa 0 — Pré-requisitos
**Objetivo:** máquina e contas prontas.
**Tarefas:** seguir [02 — Pré-requisitos](02-pre-requisitos.md) integralmente.
**Commits:** nenhum.
**Pronto quando:** todos os comandos de verificação respondem sem erro e o Minikube sobe com o nó `Ready`.

## Etapa 1 — Fundação do repositório
**Objetivo:** repositório com regras e especificação.
**Tarefas:**
1. ✅ `git init`, branch `main`.
2. ✅ `.gitignore`, `README.md`, `docs/spec/*`, `docs/ROTEIRO-VIDEO.md`.
3. Criar o repositório público: `gh repo create <owner>/conexao-solidaria --public --source . --push`.
4. Adicionar os integrantes como colaboradores.
5. Ligar secret scanning e push protection (Settings → Code security).

**Commits:** já feitos (um por arquivo).
**Pronto quando:** repositório público no GitHub com a spec visível.
**Apoio:** MCP GitHub ou `gh`.

## Etapa 2 — Planejamento ágil e DDD
**Objetivo:** backlog pronto, domínio modelado, decisões registradas.
**Tarefas:**
1. GitHub Project com colunas, campos e labels da [seção 03](03-processo-agil.md).
2. Issues H01–H10 e T01–T04 com critérios Gherkin; estimar com Planning Poker.
3. `docs/agil/definition-of-ready.md` e `docs/agil/definition-of-done.md`.
4. Event Storming no Miro (fluxos "criar campanha" e "doar"); exportar PNGs.
5. Domain Storytelling da doação; exportar PNG.
6. `docs/ddd/glossario.md` e `docs/ddd/context-map.md`.
7. ADRs 0001–0016 em `docs/adr/` (um arquivo por ADR, a partir da [seção 08](08-decisoes-arquiteturais.md)).
8. `.github/ISSUE_TEMPLATE/historia.md` e `.github/pull_request_template.md`.

**Commits (exemplos):**
```text
📝 docs(agil): adiciona definition of ready
📝 docs(agil): adiciona definition of done
📝 docs(ddd): adiciona event storming do fluxo de doação
📝 docs(ddd): adiciona event storming do fluxo de campanha
📝 docs(ddd): adiciona domain storytelling da doação
📝 docs(ddd): adiciona glossário da linguagem ubíqua
📝 docs(ddd): adiciona mapa de contextos
📝 docs(adr): adiciona ADR 0001 sobre .NET 10   (… um por ADR)
📝 docs: adiciona modelo de issue de história
📝 docs: adiciona modelo de pull request
```
**Pronto quando:** Sprint 1 planejada com itens na coluna "Pronto (DoR)".
**Apoio:** MCP GitHub; `wiki:disciplinas/ddd`, `wiki:disciplinas/metodologias-ageis`.

## Etapa 3 — Esqueleto da solução e qualidade automática
**Objetivo:** solução vazia compilando, hooks e CI mínimo funcionando.
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
✅ test: cria projetos de teste com xUnit v3
✅ test: adiciona teste de arquitetura das camadas
🔧 chore: adiciona manifesto de ferramentas dotnet
🔧 chore: adiciona hooks do husky para build, formato e mensagem
🔧 chore: adiciona script de validação da mensagem de commit
🔧 chore: adiciona configuração de cobertura com coverlet
🔧 chore: adiciona script de verificação da cobertura mínima de 90%
👷 ci: adiciona workflow com restore, formato, build, testes e cobertura
📝 docs: adiciona CLAUDE.md
🔧 chore: adiciona script de verificação de pré-requisitos
```
Cada projeto novo entra na `.slnx` no mesmo commit em que é criado (`dotnet sln add`).
**Pronto quando:** `dotnet build -warnaserror` e `dotnet test` passam local e no CI; commit com mensagem fora do padrão é bloqueado; push e CI com cobertura abaixo de 90% falham.
**Apoio:** `/init`, `/fewer-permission-prompts`, MCP Microsoft Learn.

## Etapa 4 — Identity.Api
**Objetivo:** RF01, RF03 e RF08 funcionando com testes.
**Tarefas:**
1. ServiceDefaults: Serilog, OpenTelemetry, health checks, `ProblemDetails`/`IExceptionHandler`, `/metrics`, autenticação JWT (validação).
2. Domínio (TDD): `Email`, `Cpf`, `Senha`, `Consentimento`, `Usuario`.
3. Aplicação: `CadastrarDoador`, `Autenticar`, `ObterMeusDados`, `AnonimizarMeusDados`; validadores.
4. Infraestrutura: `IdentityDbContext`, configurações, migration, `CifradorCpf` (AES-GCM + HMAC), `HasherSenha` (BCrypt), `EmissorJwt` (RS256), bloqueio por tentativas, seed do gestor.
5. API: endpoints, OpenAPI, policies, `FallbackPolicy`.
6. Testes de integração (Testcontainers PostgreSQL).

**Commits (exemplos):**
```text
🔊 feat(defaults): adiciona logs estruturados com serilog
📈 feat(defaults): adiciona tracing com opentelemetry e endpoint de métricas
🩺 feat(defaults): adiciona health checks live e ready
🥅 feat(defaults): adiciona tratamento global de erros com problem details
✨ feat(identity): adiciona value object Email
✨ feat(identity): adiciona value object Cpf com validação de dígitos
✨ feat(identity): adiciona value object Senha com política de segurança
✨ feat(identity): adiciona agregado Usuario com cadastro de doador
✨ feat(identity): adiciona anonimização do usuário
🗃️ feat(identity): adiciona IdentityDbContext e migration inicial
🔒️ feat(identity): adiciona cifragem e hash do cpf
🛂 feat(identity): adiciona hash de senha com bcrypt
🛂 feat(identity): emite jwt rs256 no login
🦺 feat(identity): adiciona validadores de cadastro e login
✨ feat(identity): adiciona endpoint de cadastro de doador
✨ feat(identity): adiciona endpoint de login
🛂 feat(identity): adiciona bloqueio por tentativas de login
✨ feat(identity): adiciona endpoints de acesso e anonimização do titular
🌱 chore(identity): adiciona seed do gestor por variáveis de ambiente
✅ test(identity): adiciona testes de integração dos endpoints
```
**Pronto quando:** cenários Gherkin de RF01, RF03 e RF08 passam; `/security-review` sem pendências críticas.
**Apoio:** `/security-review`, MCP Microsoft Learn, `wiki:disciplinas/api-dotnet`, `wiki:conceitos/jwt`.

## Etapa 5 — Campanhas.Api
**Objetivo:** RF02, RF04, RF05 (lado da API) e RF06.
**Tarefas:**
1. Domínio (TDD): `Dinheiro`, `Periodo`, `Campanha` com todas as regras e eventos de domínio.
2. Contratos: `DoacaoRecebidaEvent`, `CampanhaAlteradaEvent`, `DoacaoProcessadaEvent` + testes de snapshot.
3. Infraestrutura: `CampanhasDbContext`, configurações, `xmin`, migration (inclui tabelas do MassTransit, `doacoes_processadas` e `chaves_idempotencia`), MassTransit com Outbox, `HybridCache` + Redis, cliente Elasticsearch, consulta Dapper do painel.
4. Aplicação: casos de uso de campanha, painel, busca e `ReceberDoacao`.
5. API: endpoints com policies, `ETag`/`If-Match`, `Idempotency-Key`.
6. Consumidor `IndexarCampanhaConsumer` e endpoint de reindexação.
7. Testes de integração e cenários BDD de RF02, RF04, RF05 (API) e RF06.

**Commits (exemplos):**
```text
✨ feat(campanhas): adiciona value objects Dinheiro e Periodo
✨ feat(campanhas): adiciona agregado Campanha com regras de criação
✨ feat(campanhas): adiciona transições de status da campanha
🏷️ feat(contracts): adiciona eventos de integração v1
✅ test(contracts): adiciona snapshots dos contratos de eventos
🗃️ feat(campanhas): adiciona CampanhasDbContext e migration inicial
✨ feat(campanhas): adiciona endpoints de gestão de campanhas
✨ feat(campanhas): adiciona painel de transparência com dapper
⚡️ feat(campanhas): adiciona cache do painel com hybridcache e redis
✨ feat(campanhas): adiciona recebimento de doação com outbox
✨ feat(campanhas): adiciona chave de idempotência na doação
✨ feat(campanhas): adiciona indexação de campanhas no elasticsearch
✨ feat(campanhas): adiciona busca fuzzy de campanhas
✅ test(campanhas): adiciona testes de integração da api
✅ test(bdd): adiciona cenários de gestão de campanhas e doação
```
**Pronto quando:** cenários passam; doação retorna 202 e a mensagem fica no Outbox.
**Apoio:** `/security-review`, `wiki:disciplinas/elasticsearch`, `wiki:disciplinas/nosql`.

## Etapa 6 — Arrecadacao.Worker e mensageria
**Objetivo:** RF05 de ponta a ponta (total atualizado pelo Worker).
**Tarefas:**
1. Worker Service com MassTransit (consumidor de `DoacaoRecebidaEvent`), retry e DLQ.
2. Caso de uso `ProcessarDoacao`: transação com idempotência + `UPDATE` atômico + Outbox de `DoacaoProcessadaEvent`.
3. Livro de doações no MongoDB (upsert idempotente) e verificação na reentrega.
4. Invalidação do cache do painel.
5. Métricas de negócio, health checks e `/metrics` na porta 9090.
6. Testes: unidade do caso de uso; integração com Testcontainers e MassTransit Test Harness (inclui reentrega e falha permanente → `_error`).

**Commits (exemplos):**
```text
✨ feat(worker): adiciona consumidor de doação recebida
✨ feat(worker): soma doação ao total com idempotência
✨ feat(worker): adiciona livro de doações no mongodb
⚡️ feat(worker): invalida cache do painel após processar doação
✨ feat(worker): publica DoacaoProcessadaEvent via outbox
🥅 feat(worker): configura retentativas e fila de erro
📈 feat(worker): adiciona métricas de doações processadas
✅ test(worker): adiciona testes de reentrega e falha permanente
✅ test(bdd): adiciona cenário de painel atualizado após doação
```
**Pronto quando:** doação feita na API aparece no painel em até 5 s; evento duplicado não soma duas vezes.
**Apoio:** `wiki:disciplinas/mensageria`, `wiki:conceitos/idempotencia`.

## Etapa 7 — Notificacoes.Function
**Objetivo:** RF07 com escala a zero.
**Tarefas:**
1. Spike (time-box 1 dia): gatilho RabbitMQ no isolated worker .NET 10 lendo o envelope do MassTransit.
2. Função `EmitirRecibo` com log estruturado mascarado e deduplicação.
3. `deploy/rabbitmq/definitions.json` com a fila `notificacoes-recibo` e o binding.
4. Dockerfile da função.
5. Testes da desserialização do envelope e do conteúdo do log.

**Commits (exemplos):**
```text
✨ feat(notificacoes): adiciona função de recibo com gatilho rabbitmq
🔧 chore(notificacoes): adiciona definição da fila de recibos
🧱 build(notificacoes): adiciona dockerfile da função
✅ test(notificacoes): adiciona testes do envelope e do recibo
```
**Pronto quando:** com docker-compose, cada doação gera um log `ReciboEmitido` sem dados pessoais.
**Apoio:** `wiki:disciplinas/serverless-apps`.

## Etapa 8 — Frontend Blazor (+ IA bônus)
**Objetivo:** interface para todas as funcionalidades.
**Tarefas:**
1. Layout, navegação e páginas públicas (início, transparência, busca) com SSR estático.
2. Cadastro e login (cookie de autenticação no servidor; JWT anexado por `DelegatingHandler`).
3. Doação (`InteractiveServer`), com mensagem de "processando".
4. Gestão de campanhas (`<AuthorizeView Roles="GestorONG">`).
5. Página "Meus dados" (acesso e anonimização).
6. Testes bUnit.
7. Bônus: assistente de descrição (Semantic Kernel + Ollama) com aviso de IA e revisão humana.

**Commits (exemplos):**
```text
💄 feat(web): adiciona layout e navegação
✨ feat(web): adiciona página de transparência
✨ feat(web): adiciona busca de campanhas
🛂 feat(web): adiciona cadastro e login de doador
✨ feat(web): adiciona fluxo de doação
✨ feat(web): adiciona gestão de campanhas para gestor
✨ feat(web): adiciona página de dados do titular
✅ test(web): adiciona testes de componentes com bunit
🤖 feat(web): adiciona sugestão de descrição com semantic kernel
```
**Pronto quando:** fluxo completo pelo navegador; testes bUnit passando.
**Apoio:** MCP Playwright, `wiki:disciplinas/blazor-razor`.

## Etapa 9 — Testes complementares
**Objetivo:** cobertura de aceitação completa e metas de desempenho verificadas.
**Tarefas:** cenários BDD restantes; k6 (`painel.js`, `doacao.js`, `busca.js`); testes que faltarem para manter cada projeto com cobertura ≥ 90%; bônus GraphQL/gRPC se houver tempo; E2E Playwright (bônus).
**Commits (exemplos):**
```text
✅ test(bdd): adiciona cenários de busca e recibo
✅ test(load): adiciona teste de carga do painel com k6
✅ test(load): adiciona teste de carga da doação com k6
✅ test(load): adiciona teste de carga da busca com k6
```
**Pronto quando:** todos os RFs com cenário automatizado; thresholds do k6 atendidos (após a Etapa 11).
**Apoio:** `wiki:disciplinas/qualidade-software`.

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
**Objetivo:** tudo rodando no Minikube com os `.yaml` exigidos.
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
**Objetivo:** pipeline completo e `main` protegida.
**Tarefas:** completar `ci.yml` (cobertura, gitleaks, matriz Docker, Trivy, push GHCR); `codeql.yml`; `dependabot.yml`; proteção da `main`; `cd.yml` opcional.
**Commits (exemplos):**
```text
👷 ci: adiciona relatório de cobertura no resumo do workflow
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
**Tarefas:** `/security-review` completo; checklist OWASP da [seção 11.7](11-seguranca-e-lgpd.md#117-checklist-owasp-top-10-revisão-na-etapa-16); registro de tratamento; política de privacidade; conferir logs reais em busca de dados pessoais.
**Commits (exemplos):**
```text
📝 docs(lgpd): adiciona registro das operações de tratamento
📝 docs(lgpd): adiciona política de privacidade
🔒️ fix(...): <correções apontadas na revisão>
```
**Pronto quando:** checklist OWASP e LGPD 100%; gitleaks e Trivy verdes.

## Etapa 17 — Documentação e entregáveis
**Objetivo:** tudo o que o avaliador precisa.
**Tarefas:**
1. README final: visão, arquitetura, pré-requisitos, passo a passo com docker-compose e com Kubernetes, URLs, usuários de teste, variáveis, como rodar testes, links.
2. `docs/arquitetura/arquitetura.png` (exportado do Mermaid).
3. `docs/justificativa-bancos.md` e o PDF gerado.
4. Coleção e ambiente Postman.
5. Relatório de entrega (PDF): grupo, participantes, usernames do Discord, links do repositório, vídeo e documentação.

**Commits (exemplos):**
```text
📝 docs: adiciona diagrama de arquitetura em png
📝 docs: adiciona justificativa dos bancos de dados
📝 docs: adiciona pdf da justificativa dos bancos
📝 docs: adiciona coleção postman
📝 docs: atualiza README com passo a passo completo
📝 docs: adiciona relatório de entrega
```
**Pronto quando:** outra pessoa sobe o projeto do zero seguindo só o README.
**Apoio:** skill `pdf`.

## Etapa 18 — Vídeo
**Objetivo:** vídeo de até 15 minutos comprovando a arquitetura.
**Tarefas:** seguir [ROTEIRO-VIDEO.md](../ROTEIRO-VIDEO.md): preparação, ensaio cronometrado, gravação, revisão, publicação.
**Commits:** `📝 docs: adiciona link do vídeo no README`.
**Pronto quando:** vídeo ≤ 15:00 publicado e link testado em aba anônima.

## Etapa 19 — Conclusão
**Objetivo:** entrega final.
**Tarefas:**
1. Checklist de [21 — Entrega e riscos](21-entrega-e-riscos.md) 100%.
2. `git tag -a v1.0.0` + push; CI publica as imagens `1.0.0`; `gh release create v1.0.0 --generate-notes`.
3. Confirmar repositório público e README funcionando.
4. Apagar recursos de cloud, se houver.
5. Entregar o relatório na plataforma da FIAP.
6. Retrospectiva final do projeto.

**Commits:** `🔖 chore: versão 1.0.0` (atualiza versão nos `Directory.Build.props`).
**Pronto quando:** entrega registrada na plataforma.
