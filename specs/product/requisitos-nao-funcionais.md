# 05 — Requisitos não funcionais

[← README](../../README.md) · Anterior: [04 — Requisitos funcionais](../../docs/spec/04-requisitos-funcionais.md) · Próximo: [06 — Modelo de domínio](dominio.md)

Metas medidas no ambiente local (Minikube) com os recursos da [seção 13](../../docs/spec/13-infraestrutura.md). Os números são de referência para um MVP e são verificados pelo teste de carga (k6) e pelo dashboard.

## 5.1 Desempenho

| ID | Requisito | Meta | Como verificar |
|---|---|---|---|
| RNF01 | Latência do painel de transparência | p95 < 300 ms com 50 usuários virtuais | k6 `painel.js` com threshold |
| RNF02 | Latência do `POST /doacoes` | p95 < 500 ms com 20 usuários virtuais | k6 `doacao.js` com threshold |
| RNF03 | Tempo até o total aparecer no painel | < 5 s em 95% das doações | Cenário BDD do RF04 e painel "Atraso de processamento" |
| RNF04 | Busca fuzzy | p95 < 400 ms | k6 `busca.js` |

## 5.2 Escalabilidade

| ID | Requisito | Mecanismo |
|---|---|---|
| RNF05 | APIs escalam horizontalmente sem estado local | Sessão no JWT, cache no Redis, HPA por CPU (70%) |
| RNF06 | Worker escala pelo tamanho da fila | KEDA com gatilho RabbitMQ (`doacao-recebida`), 1 a 4 réplicas |
| RNF07 | Função de notificação escala a zero | KEDA, 0 a 3 réplicas |
| RNF08 | Pico de doações não derruba a API | A API só grava no Outbox; o processamento pesado fica no Worker |

## 5.3 Disponibilidade e resiliência

| ID | Requisito | Mecanismo |
|---|---|---|
| RNF09 | APIs com pelo menos 2 réplicas | `replicas: 2`, `PodDisruptionBudget` `minAvailable: 1` |
| RNF10 | Deploy sem indisponibilidade | `RollingUpdate` com `maxUnavailable: 0`, `maxSurge: 1`, readiness probe |
| RNF11 | Nenhuma doação aceita é perdida | Transactional Outbox, filas e mensagens duráveis, DLQ |
| RNF12 | Nenhuma doação é somada duas vezes | Idempotência por `doacaoId` no Worker |
| RNF13 | Falha do Elasticsearch ou do Redis não derruba as funções principais | Busca responde 503; painel lê do banco se o cache falhar |
| RNF14 | Chamadas HTTP com timeout, retry e circuit breaker | `Microsoft.Extensions.Http.Resilience` no Blazor |
| RNF15 | Pod com falha é reiniciado sozinho | Liveness probe |

## 5.4 Segurança

| ID | Requisito |
|---|---|
| RNF16 | Todo endpoint não público exige JWT válido; autorização por role verificada no serviço |
| RNF17 | Senhas com BCrypt; CPF cifrado em repouso |
| RNF18 | Nenhuma credencial no código, YAML versionado ou imagem |
| RNF19 | HTTPS no gateway (certificado local via mkcert) |
| RNF20 | Rate limiting: 60 req/min por IP nas rotas públicas; 10 req/min no login |
| RNF21 | Containers sem root e com sistema de arquivos somente leitura |
| RNF22 | Nenhuma vulnerabilidade `CRITICAL` corrigível nas imagens (Trivy) |
| RNF23 | Nenhum segredo no histórico do git (gitleaks) |

Detalhes: [11 — Segurança e LGPD](../../docs/spec/11-seguranca-e-lgpd.md).

## 5.5 Privacidade (LGPD)

| ID | Requisito |
|---|---|
| RNF24 | Logs, métricas, traces e eventos sem dados pessoais (só IDs) |
| RNF25 | Coleta mínima: apenas os campos exigidos pelo enunciado |
| RNF26 | Direitos de acesso e eliminação atendidos pela API (RF08) |
| RNF27 | Base legal documentada para cada finalidade |

## 5.6 Observabilidade

| ID | Requisito |
|---|---|
| RNF28 | `/health/live`, `/health/ready` e `/metrics` em todos os serviços |
| RNF29 | Logs estruturados em JSON com `traceId` |
| RNF30 | Trace distribuído da doação: API → RabbitMQ → Worker → Função |
| RNF31 | Dashboard Grafana com métricas reais (requisições, latência, erros, CPU e memória, doações, fila) |
| RNF32 | Alertas para erros 5xx, mensagens na DLQ e pods reiniciando |

Detalhes: [12 — Observabilidade](../../docs/spec/12-observabilidade.md).

## 5.7 SLOs (indicativos)

| SLI | SLO | Janela |
|---|---|---|
| Disponibilidade do painel (respostas não 5xx) | 99,5% | 7 dias |
| Latência do painel (p95) | < 300 ms | 7 dias |
| Doações processadas em até 5 s | 95% | 7 dias |

Orçamento de erro do painel: 0,5% das requisições. Ver `wiki:conceitos/sli-slo-sla` e `wiki:conceitos/error-budget`.

## 5.8 Manutenibilidade

| ID | Requisito |
|---|---|
| RNF33 | Build sem avisos (`TreatWarningsAsErrors`) e formato verificado no CI |
| RNF34 | Cobertura de testes **≥ 90% de linhas e ≥ 90% de ramos**, no total e em cada projeto de `src/`; abaixo disso o push e o CI falham |
| RNF35 | Domínio sem dependência de frameworks ou infraestrutura |
| RNF36 | Toda decisão arquitetural registrada em ADR |
| RNF37 | Todo commit compila e passa nos testes |

## 5.9 Portabilidade e operação

| ID | Requisito |
|---|---|
| RNF38 | A stack inteira sobe com um comando: `docker compose up -d` ou `kubectl apply -k` |
| RNF39 | Configuração por variáveis de ambiente (12-factor) |
| RNF40 | Imagens com tag imutável (`sha-<commit>`), nunca `latest` |
| RNF41 | README permite subir o projeto numa máquina limpa seguindo só o passo a passo |
