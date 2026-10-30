# 08 — Decisões arquiteturais (ADRs)

[← Índice](../SPEC.md) · Anterior: [07 — Arquitetura](07-arquitetura.md) · Próximo: [09 — API e eventos](09-api-e-eventos.md)

Cada decisão abaixo vira um arquivo `docs/adr/NNNN-titulo.md` na Etapa 2, no formato: **Status, Contexto, Decisão, Alternativas, Consequências**. Novas decisões durante o projeto recebem novos ADRs; ADRs aceitos não são editados, são substituídos por outro (status `Substituído por NNNN`).

---

## ADR 0001 — .NET 10 LTS
- **Contexto:** o enunciado da Fase 2 pede .NET 8 ou superior. O .NET 8 sai de suporte em novembro de 2026.
- **Decisão:** usar .NET 10 (LTS) em todos os projetos, com o SDK fixado em `global.json`.
- **Alternativas:** .NET 8 (suporte acabando); .NET 9 (STS, já fora de suporte).
- **Consequências:** acesso a `HybridCache`, `.slnx`, melhorias de Minimal API e OpenAPI nativo. Exceção possível só na função serverless (ADR 0010).

## ADR 0002 — Monorepo
- **Contexto:** o Hackathon pede **um** repositório público com README passo a passo.
- **Decisão:** um repositório, uma solução `ConexaoSolidaria.slnx`, projetos isolados por serviço e um Dockerfile por serviço.
- **Alternativas:** um repositório por serviço (como na Fase 2): mais burocracia para avaliar e para o CI.
- **Consequências:** CI com matriz por serviço; o teste de arquitetura impede referências cruzadas entre contextos.

## ADR 0003 — Três bounded contexts e quatro componentes de backend
- **Contexto:** o enunciado exige ≥ 2 microsserviços. Excesso de serviços aumenta o custo de operação local.
- **Decisão:** contextos Identidade, Campanhas e Notificações; componentes `Identity.Api`, `Campanhas.Api`, `Arrecadacao.Worker` e `Notificacoes.Function`.
- **Alternativas:** 2 serviços (API única + Worker): cumpriria o mínimo, mas misturaria identidade com arrecadação; um serviço por agregado: complexidade sem ganho.
- **Consequências:** fronteiras alinhadas ao negócio; comunicação entre contextos só por JWT e eventos.

## ADR 0004 — Worker como único escritor do total arrecadado
- **Contexto:** o enunciado exige que o Worker atualize o "Valor Total Arrecadado" e que a API não o faça.
- **Decisão:** `Arrecadacao.Worker` pertence ao contexto Campanhas, compartilha o `campanhas_db` com a `Campanhas.Api` e é o único que escreve `total_arrecadado`. A API só lê.
- **Alternativas:** Worker com banco próprio e evento de volta para a API atualizar o total: a API voltaria a atualizar o total, contrariando o enunciado.
- **Consequências:** "um contexto, dois deployables": escalam separadamente. O banco é do contexto, não de cada processo. As migrations ficam no `Campanhas.Infrastructure`.

## ADR 0005 — Doação assíncrona com Transactional Outbox
- **Contexto:** publicar no broker e gravar no banco em passos separados pode perder eventos (falha entre os dois).
- **Decisão:** a API grava o evento na tabela de Outbox do MassTransit na mesma transação e responde 202. Um serviço em segundo plano entrega o evento ao RabbitMQ.
- **Alternativas:** publicar direto no RabbitMQ (risco de perda); two-phase commit (não suportado e caro).
- **Consequências:** entrega *at-least-once*; consumidores precisam ser idempotentes (ADR 0006); pequeno atraso na entrega (polling do Outbox, ~1 s).

## ADR 0006 — Consumidores idempotentes
- **Contexto:** at-least-once implica mensagens duplicadas.
- **Decisão:** tabela `doacoes_processadas` com PK `doacao_id`, inserida na mesma transação do `UPDATE` do total. Duplicata → violação de PK → mensagem confirmada sem efeito. MongoDB com upsert por `_id = doacaoId`. Função com deduplicação por `doacaoId`.
- **Alternativas:** Inbox do MassTransit (deduplica por `MessageId`, não por regra de negócio; usado como camada extra).
- **Consequências:** reentregas são seguras; o teste "evento duplicado" é obrigatório.

## ADR 0007 — Persistência poliglota
- **Contexto:** padrões de acesso diferentes (transação financeira, histórico imutável, leitura pública intensa, busca textual). A Fase 4 exige NoSQL, cache e motor de busca.
- **Decisão:** PostgreSQL (dados transacionais), MongoDB (livro de doações), Redis (cache do painel), Elasticsearch (busca). Detalhes em [10 — Dados](10-dados.md).
- **Alternativas:** só PostgreSQL (com `tsvector` para busca e tabela de auditoria): mais simples, mas não cobre NoSQL nem Elasticsearch; SQL Server: imagem mais pesada, sem vantagem aqui.
- **Consequências:** mais componentes para operar; justificativa formal no PDF exigido pelo enunciado.

## ADR 0008 — JWT RS256 emitido só pela Identity
- **Contexto:** com chave simétrica (HS256), qualquer serviço que valida também poderia emitir tokens.
- **Decisão:** Identity assina com chave privada RSA 2048 (Secret); os demais validam com a chave pública (ConfigMap). Validade de 60 min; validação de `iss`, `aud`, `exp` e assinatura com tolerância de 30 s.
- **Alternativas:** HS256 (mais simples, menos seguro); provedor OIDC externo (Keycloak): fora do escopo do MVP.
- **Consequências:** rotação de chave exige atualizar Secret e ConfigMap; documentado no runbook.

## ADR 0009 — Kong como API Gateway
- **Contexto:** bônus do enunciado; conteúdo da Fase 3 (Kong e Azure APIM).
- **Decisão:** Kong Ingress Controller em modo DB-less, configurado por recursos Kubernetes (`Ingress`, `KongPlugin`) versionados.
- **Alternativas:** Ocelot ou YARP (código .NET; não é conteúdo do curso); Azure APIM (pago, só na cloud); NGINX Ingress (sem plugins de API).
- **Consequências:** rate limiting, CORS, correlation-id e métricas sem código; a Admin API nunca é exposta.

## ADR 0010 — Notificações como função serverless com KEDA
- **Contexto:** a Fase 3 pede funções serverless acionadas por eventos. O ambiente principal é local.
- **Decisão:** `Notificacoes.Function` em Azure Functions isolated worker com gatilho RabbitMQ, empacotada como container e escalada de 0 a 3 réplicas pelo KEDA. Na cloud opcional, a mesma imagem roda no AKS com KEDA.
- **Alternativas:** AWS Lambda com LocalStack (exigiria ponte RabbitMQ → SQS); Function no plano Consumption da Azure (gatilho RabbitMQ não é suportado nesse plano).
- **Consequências:** modelo de programação serverless e escala a zero, rodando localmente. Se o runtime do Functions não suportar .NET 10, a função usa .NET 8 isolated.

## ADR 0011 — Observabilidade com Prometheus, Grafana e Jaeger
- **Contexto:** o enunciado pede `/health`, `/metrics` e dashboard Grafana com métricas reais. A Fase 3 cobre Prometheus, Grafana, Zabbix, APMs e tracing.
- **Decisão:** prometheus-net para `/metrics`; OpenTelemetry para traces (exportados via OTLP para o Jaeger); Serilog em JSON no stdout; kube-prometheus-stack (Prometheus, Grafana, Alertmanager, kube-state-metrics, node-exporter). Zabbix opcional.
- **Alternativas:** Datadog ou New Relic (SaaS; dependem de conta e custo); OpenTelemetry também para métricas (o exporter Prometheus do OTel .NET ainda é pré-lançamento).
- **Consequências:** stack gratuita e local; dashboards e alertas versionados como código.

## ADR 0012 — RabbitMQ com MassTransit 8.x
- **Contexto:** o enunciado permite RabbitMQ ou Kafka. O MassTransit v9 passou a ter licença comercial.
- **Decisão:** RabbitMQ 4 com plugin de management e Prometheus; MassTransit 8.x (Apache 2.0) fixado no `Directory.Packages.props`.
- **Alternativas:** Kafka (mais pesado no Minikube); `RabbitMQ.Client` puro (reimplementar retry, Outbox e DLQ).
- **Consequências:** Outbox, retry, DLQ e tracing prontos; atualizações limitadas à linha 8.x.

## ADR 0013 — Bibliotecas com licença comercial evitadas
- **Contexto:** MediatR, AutoMapper, FluentAssertions e MassTransit v9 mudaram para licenças comerciais.
- **Decisão:** casos de uso chamados diretamente (sem mediator); mapeamento manual em métodos de extensão; asserções com Shouldly; MassTransit 8.x.
- **Consequências:** menos dependências e mais código explícito.

## ADR 0014 — GitHub Actions e GHCR
- **Contexto:** o enunciado exige CI a cada push na branch principal com build e imagem Docker. Repositório público no GitHub.
- **Decisão:** GitHub Actions; imagens no GitHub Container Registry (GHCR) com tag `sha-<commit>` e `vX.Y.Z`.
- **Alternativas:** Azure DevOps (Pipelines e Boards): exigiria projeto separado; Docker Hub: limites de pull.
- **Consequências:** Azure DevOps e AWS ficam como alternativas registradas, não implementadas.

## ADR 0015 — Execução local com cloud opcional na Azure
- **Contexto:** o Hackathon pede Kubernetes local. As Fases 3 e 4 trabalharam com cloud, que gera custo.
- **Decisão:** ambiente oficial é o Minikube. A Etapa 15 (opcional) implanta na Azure (AKS, ACR, Key Vault) com Bicep, e a infraestrutura é apagada depois da gravação.
- **Alternativas:** AWS (EKS, ECR); cloud obrigatória.
- **Consequências:** sem custo obrigatório; overlays de Kustomize separam local e Azure.

## ADR 0016 — Frontend Blazor Web App
- **Contexto:** a Fase 5 ensina Blazor e Razor. O enunciado aceita Postman ou Swagger, mas um frontend melhora a demonstração.
- **Decisão:** Blazor Web App com SSR estático nas páginas públicas e `InteractiveServer` nos formulários. O JWT fica no servidor (cookie de autenticação do Blazor); nunca no `localStorage`.
- **Alternativas:** Blazor WebAssembly (token no navegador, maior download); SPA em JavaScript (fora do curso).
- **Consequências:** circuito SignalR por usuário; afinidade de sessão no Kong para o Web com 2 réplicas.
