# 21 — Entrega e riscos

[← README](../README.md) · Anterior: [20 — Plano de execução](plano-de-execucao.md) · Próximo: [22 — Manual de implementação](spec/22-manual-de-implementacao.md)

## 21.1 Checklist do enunciado (com evidência)

| # | Exigência do enunciado | Evidência | Onde mostrar no vídeo |
|---|---|---|---|
| 1 | RBAC com JWT (`GestorONG`, `Doador`) | Testes de 401/403; token com `role` | Bloco 6 |
| 2 | Gestão de campanhas com regras (data fim, meta) | Cenários Gherkin RF02 | Bloco 7 |
| 3 | Cadastro de doador (e-mail único, CPF válido, senha com hash) | Cenários RF03; coluna `senha_hash` | Bloco 8 |
| 4 | Painel só com campanhas ativas e total arrecadado | Cenário RF04 | Blocos 7 e 8 |
| 5 | Doação só para campanha ativa | Cenário RF05 (422) | Bloco 8 |
| 6 | ≥ 2 microsserviços | 4 componentes de backend | Blocos 2 e 4 |
| 7 | API publica `DoacaoRecebidaEvent`, não atualiza o total | Código do caso de uso + teste "total não alterado pela API" | Blocos 2 e 8 |
| 8 | Worker consome e atualiza o total | Painel atualizado; logs do Worker | Bloco 8 |
| 9 | Kubernetes local com `.yaml` (Deployments, Services, ConfigMaps) | `deploy/k8s/` | Bloco 4 |
| 10 | Métricas em `/health` ou `/metrics` | Endpoints em todos os serviços | Bloco 5 |
| 11 | Dashboard Grafana com métricas reais | `conexao-solidaria.json` | Bloco 5 e 8 |
| 12 | CI a cada push na `main` com build e imagem Docker | `ci.yml` + GHCR | Bloco 3 |
| 13 | Bônus: testes de unidade no CI | Job `build-test` | Bloco 3 |
| 14 | Bônus: API Gateway | Kong | Blocos 2 e 6 |
| 15 | Repositório público com README passo a passo | GitHub | Bloco 11 |
| 16 | Diagrama de arquitetura | `docs/arquitetura/arquitetura.png` | Bloco 2 |
| 17 | PDF justificando os bancos | `docs/justificativa-bancos.pdf` | Bloco 2 |
| 18 | Vídeo até 15 min | Link no README e no relatório | — |
| 19 | Relatório com grupo, Discord e links | PDF na plataforma | — |

## 21.2 Checklist de qualidade

- [ ] Todos os commits no padrão da [seção 17](git-e-commits.md) e compilando (`git rebase --exec` na `main` inteira sem falha).
- [ ] CI verde na `main`.
- [ ] Nenhum segredo no histórico (gitleaks).
- [ ] Nenhuma vulnerabilidade crítica corrigível (Trivy).
- [ ] Cobertura ≥ 90% de linhas e ramos, no total e em cada projeto de `src/` (relatório do último CI da `main`).
- [ ] Thresholds do k6 atendidos.
- [ ] Checklists de segurança e LGPD completos.
- [ ] Todos os ADRs escritos.
- [ ] README testado numa máquina limpa (ou num clone novo).
- [ ] Recursos de cloud apagados (se usados).

## 21.3 Modelo do relatório de entrega

```text
Hackathon — Fase 5 — PósTech Arquitetura de Sistemas .NET
Projeto: Conexão Solidária

Grupo: <nome/número>
Integrantes:
  - <Nome completo> — RM <número> — Discord: <username>
  - ...

Links:
  - Repositório: https://github.com/<owner>/conexao-solidaria
  - Vídeo: <link>
  - Diagrama de arquitetura: <link para docs/arquitetura/arquitetura.png>
  - Justificativa dos bancos (PDF): <link>
  - Especificação: <link para docs/spec/>
  - Event Storming (Miro): <link>
```

## 21.4 Riscos

| # | Risco | Probabilidade | Impacto | Mitigação | Plano de contingência |
|---|---|---|---|---|---|
| R1 | Memória insuficiente para toda a stack no Minikube | Média | Alto | Heap do Elasticsearch em 512 MB; limites definidos; Zabbix opcional | Desligar Jaeger e Zabbix na gravação; Elasticsearch fora do cluster (docker-compose) |
| R2 | Runtime do Azure Functions sem suporte a .NET 10 | Média | Médio | Spike na Etapa 7 | Função em .NET 8 isolated (ADR 0010) |
| R3 | Gatilho RabbitMQ da função não lê o envelope do MassTransit | Baixa | Médio | Spike na Etapa 7; fila declarada por `definitions.json` | Configurar o MassTransit para publicar JSON "cru" nesse evento |
| R4 | Licença comercial em bibliotecas | Baixa | Médio | Versões fixadas; lista de pacotes proibidos (ADR 0013) | Trocar pela alternativa da [seção 16.2](padroes-de-codigo.md#162-pacotes-aprovados) |
| R5 | Custo na Azure | Baixa | Alto | Cloud opcional; alerta de orçamento; exclusão após gravação | Pular a Etapa 15 |
| R6 | Total exibido desatualizado na demonstração | Média | Baixo | Consistência eventual explicada; atraso < 5 s | Repetir o `GET` após alguns segundos (Plano B do roteiro) |
| R7 | Vídeo acima de 15 min | Média | Alto | Roteiro cronometrado e ensaio | Cortar blocos 9 e 10 para versões curtas |
| R8 | CI lento (Testcontainers + matriz Docker) | Média | Baixo | Cache de NuGet e de camadas; `concurrency` | Usar execução já concluída no vídeo |
| R9 | Commit quebrado chega à `main` | Baixa | Médio | Hooks + `rebase --exec` + CI obrigatório | Correção imediata com `🚑️ fix` |
| R10 | Integrante sem máquina com 16 GB | Média | Médio | docker-compose como alternativa de desenvolvimento | Usar o Kubernetes só na máquina de quem grava |
| R11 | Mudança de versão de ferramenta quebra o ambiente | Baixa | Médio | Versões fixadas (`global.json`, lock files, tags de imagem) | Voltar para a versão anterior registrada |
| R12 | Atraso no cronograma | Média | Alto | Sprints com metas claras; bônus por último | Cortar bônus (GraphQL, gRPC, IA, Zabbix, Azure) sem afetar o obrigatório |

## 21.5 Ordem de corte (se faltar tempo)

Cortar de cima para baixo, sem tocar nos itens obrigatórios:

1. Cloud Azure (Etapa 15)
2. IA (assistente de descrição)
3. gRPC e GraphQL
4. Zabbix
5. Playwright E2E
6. Busca fuzzy (Elasticsearch) — avisar no vídeo que é conteúdo da Fase 4 deixado de fora
7. Função serverless — o recibo passa a ser um log no Worker
