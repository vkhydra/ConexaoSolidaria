# Roteiro do vídeo — Conexão Solidária

Vídeo de entrega do Hackathon da Fase 5. **Duração máxima: 15 minutos.** O foco é comprovar a arquitetura, na ordem exigida pelo enunciado.

## Preparação (antes de gravar)

- [ ] Cluster no ar: `minikube start` e `kubectl get pods -A` com todos os pods `Running`.
- [ ] Port-forwards ou túnel abertos: Kong (API), Grafana, Prometheus, RabbitMQ Management, Jaeger e Blazor.
- [ ] Campanha "Natal Solidário" **não** criada (será criada ao vivo). Uma campanha "Inverno 2026" já cancelada, para o cenário de erro.
- [ ] Usuário `GestorONG` de seed funcionando; nenhum doador "Maria" cadastrado.
- [ ] Coleção Postman (`docs/postman/`) com variáveis de ambiente prontas.
- [ ] Um commit pequeno preparado para disparar o CI ao vivo (ex.: texto no README).
- [ ] Dashboard do Grafana aberto, intervalo "Last 15 minutes", atualização a cada 5 s.
- [ ] Notificações do sistema desligadas; fonte do terminal e do navegador ampliada; nenhum segredo visível (`.env`, valores de Secrets).
- [ ] OBS: 1080p, áudio testado, cenas "Tela cheia" e "Câmera + tela".
- [ ] Ensaio cronometrado feito pelo menos uma vez.

## Roteiro

| Tempo | Bloco | Tela | O que mostrar e falar |
|---|---|---|---|
| 00:00–00:40 | 1. Abertura | Câmera + slide de título | Grupo e integrantes. O problema: a ONG Esperança Solidária gere doadores e campanhas de forma manual. O objetivo: MVP escalável, observável e automatizado. |
| 00:40–03:00 | 2. Arquitetura | `docs/arquitetura/arquitetura.png` | Percorrer o diagrama: Kong → Identity.Api e Campanhas.Api → RabbitMQ → Arrecadacao.Worker → Notificacoes.Function. Por que a doação é assíncrona (a API não atualiza o total; o Worker sim; consistência eventual). Bancos e motivo de cada um (PostgreSQL, MongoDB, Redis, Elasticsearch), citando o PDF de justificativa. Observabilidade (Prometheus, Grafana, Jaeger). |
| 03:00–04:30 | 3. CI/CD | GitHub Actions | Fazer push do commit preparado. Mostrar o workflow: format, build, testes com cobertura, scans e **build da imagem Docker**. Abrir uma execução já concluída e as imagens com tag `sha-*` no GHCR. |
| 04:30–05:45 | 4. Kubernetes | Terminal | `kubectl get pods -A`, `kubectl get deploy,svc,hpa -n conexao`, `kubectl get configmap -n conexao`. Mostrar um manifesto de Deployment (probes, resources, réplicas). Mencionar que os Secrets existem, mas não são versionados. |
| 05:45–07:00 | 5. Observabilidade | Grafana | Dashboard ao vivo: requisições/s, latência p95, CPU e memória dos pods, doações processadas e fila. Mostrar rapidamente `/metrics` de um serviço e o alvo no Prometheus. |
| 07:00–08:15 | 6. Login e JWT | Postman ou Swagger | Login do `GestorONG` obtendo o JWT. Mostrar a claim `role` decodificada localmente (não colar o token em sites externos). Chamar a gestão sem token (401) e com token de doador (403). |
| 08:15–09:30 | 7. Criar campanha | Postman ou Swagger | Tentativa com meta 0 → 422 (regra de domínio). Criar "Natal Solidário" válida → 201 com `Location`. Mostrar que ela aparece no painel público. |
| 09:30–12:15 | 8. Fluxo de doação (ponto central) | Postman + RabbitMQ + Grafana + Jaeger | 1) Cadastrar a doadora "Maria" (mostrar CPF inválido → 400, depois válido → 201). 2) Login da Maria. 3) Doar 150,00 para "Inverno 2026" (cancelada) → 422. 4) Doar 150,00 para "Natal Solidário" → **202**, mostrando o payload. 5) RabbitMQ Management: a mensagem passando pela fila do Worker. 6) Logs do Worker (`kubectl logs`). 7) `GET /transparencia/campanhas` com o **valor atualizado pelo Worker**. 8) Contador de doações subindo no Grafana e o trace da doação no Jaeger (API → RabbitMQ → Worker → Função). |
| 12:15–13:15 | 9. Frontend e busca | Blazor | Painel de transparência no navegador. Busca com erro de digitação ("natl solidaro") retornando a campanha (fuzzy search). |
| 13:15–14:15 | 10. Qualidade, segurança e LGPD | IDE e terminal | Testes de unidade e BDD (um `.feature`). Senha com BCrypt, CPF mascarado nos logs, eventos só com IDs, anonimização do titular. Recibo emitido pela função serverless, que escala a zero com KEDA (`kubectl get pods -w`). |
| 14:15–14:50 | 11. Encerramento | Câmera + README | Repositório público, README passo a passo e documentação. Agradecimento. |

Folga de 10 segundos para o limite de 15 minutos.

## Plano B

| Falha durante a gravação | Ação |
|---|---|
| CI demora | Mostrar a execução já concluída e seguir; voltar ao final se terminar |
| Pod reiniciando | Explicar a probe e o restart automático; seguir com as outras réplicas |
| Mensagem não aparece na fila (processamento rápido demais) | Mostrar o gráfico "Message rates" e o contador de mensagens confirmadas da fila |
| Total ainda não atualizado | Explicar a consistência eventual e repetir o `GET` após alguns segundos |

## Pós-gravação

- [ ] Conferir a duração (≤ 15:00), o áudio e se nenhum segredo apareceu.
- [ ] Publicar o vídeo (YouTube não listado ou Drive com acesso por link) e testar o link em aba anônima.
- [ ] Colocar o link no README e no relatório de entrega.
- [ ] Se a etapa de cloud foi usada, excluir os recursos da Azure.
