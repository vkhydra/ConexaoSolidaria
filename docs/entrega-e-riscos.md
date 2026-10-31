# Entrega e riscos

[← README](../README.md)

Entrega da **Atividade Substitutiva da Fase 3** ([ADR 0018](adr/0018-escopo-trabalho-substitutivo.md)). O único entregável pedido é **um vídeo** mostrando todos os passos funcionando; a avaliação olha o **fluxo de CI com a cobertura** do código.

## Checklist do enunciado (com evidência)

| # | Exigência do enunciado | Evidência | Onde mostrar no vídeo |
|---|---|---|---|
| 1 | API .NET com funcionalidades testáveis (métodos, serviços ou controladores) | Conexão Solidária: domínio, casos de uso e endpoints das features 001–008 | Bloco 2 |
| 2 | Testes unitários das funcionalidades selecionadas | Projetos `tests/unit/*.UnitTests` | Bloco 3 |
| 3 | Framework de testes do .NET (MSTest, NUnit ou xUnit) | xUnit v3 nos `.csproj` de teste | Bloco 3 |
| 4 | Casos de sucesso e cenários de erro | Testes nomeados `Metodo_Cenario_Resultado` com os dois tipos por funcionalidade | Bloco 3 |
| 5 | Mais de um teste | Contagem de testes no `dotnet test` e no relatório `.trx` | Blocos 3 e 4 |
| 6 | Pipeline de CI no GitHub Actions ou no Azure DevOps | `.github/workflows/ci.yml` (GitHub Actions) | Bloco 4 |
| 7 | A pipeline executa os testes unitários | Job `testes-unitarios` | Bloco 4 |
| 8 | A pipeline retorna a cobertura (Sonar ou dados para download) | Resumo de cobertura na execução e artefato `cobertura-testes-unitarios` | Bloco 5 |
| 9 | Vídeo com todos os passos funcionando | [Roteiro do vídeo](ROTEIRO-VIDEO.md) | — |

Especificação dessas exigências: [feature 009](../specs/features/009-ci-testes-cobertura/spec.md).

## Checklist de qualidade (padrão do projeto, além do enunciado)

- [ ] Todos os commits no padrão de [git e commits](git-e-commits.md) e compilando (`git rebase --exec` na `main` inteira sem falha).
- [ ] CI verde na `main`.
- [ ] Features com `Status: Implementada` e tarefas dos `tasks.md` marcadas (no mínimo a 009 e as features demonstradas no vídeo).
- [ ] Cobertura ≥ 90% de linhas e ramos, no total e em cada projeto de `src/` (artefato `cobertura-total` do último CI da `main`).
- [ ] Nenhum segredo no histórico (gitleaks).
- [ ] Checklists de segurança e LGPD completos para o que foi implementado.
- [ ] ADRs atualizados.
- [ ] README testado num clone novo.
- [ ] Recursos de cloud apagados (se usados).

## Entrega na plataforma

1. Publicar o vídeo (YouTube não listado ou Google Drive com acesso por link) e testar o link numa aba anônima.
2. Colocar o link do vídeo no README.
3. Enviar o link do vídeo na plataforma da FIAP (e o link do repositório, se o formulário pedir).

## Riscos

| # | Risco | Probabilidade | Impacto | Mitigação | Plano de contingência |
|---|---|---|---|---|---|
| R1 | Pipeline sem cobertura visível no vídeo (relatório vazio ou artefato ausente) | Baixa | **Alto** | Pipeline criada na Etapa 3 e validada a cada feature ([feature 009](../specs/features/009-ci-testes-cobertura/tasks.md)) | Gravar a partir de uma execução anterior já concluída |
| R2 | Testes só de caminho feliz | Média | **Alto** | Regra "sucesso e erro" em cada `tasks.md`; revisão com o agente `reviewer` | Completar os cenários de erro antes de gravar |
| R3 | Escopo grande atrasa o que é avaliado | Média | Alto | Feature 009 primeiro; aplicação das fases por último | Seguir a ordem de corte abaixo |
| R4 | Memória insuficiente para toda a stack no Minikube | Média | Médio | Heap do Elasticsearch em 512 MB; limites definidos | Demonstrar a API com docker-compose; o vídeo do substitutivo não exige Kubernetes |
| R5 | Runtime do Azure Functions sem suporte a .NET 10 | Média | Baixo | Spike na feature 007 | Função em .NET 8 isolated (ADR 0010) |
| R6 | Licença comercial em bibliotecas | Baixa | Médio | Versões fixadas; pacotes proibidos (ADR 0013) | Trocar pela alternativa da [lista de pacotes aprovados](padroes-de-codigo.md#pacotes-aprovados) |
| R7 | Custo na Azure | Baixa | Alto | Cloud opcional; alerta de orçamento | Pular a Etapa 15 |
| R8 | CI lento (Testcontainers) | Média | Baixo | Unidade e integração em jobs separados; cache do NuGet | Mostrar no vídeo o job de unidade, que é o avaliado |
| R9 | Commit quebrado chega à `main` | Baixa | Médio | Hooks + `rebase --exec` + CI obrigatório | Correção imediata com `🚑️ fix` |
| R10 | Mudança de versão de ferramenta quebra o ambiente | Baixa | Médio | Versões fixadas (`global.json`, lock files, tags de imagem) | Voltar para a versão anterior registrada |

## Ordem de corte (se faltar tempo)

Cortar de cima para baixo. **Nunca cortar** a feature 009 nem os testes de sucesso e erro das features entregues.

1. Cloud Azure (Etapa 15)
2. Assistente de IA, gRPC e GraphQL
3. Zabbix e Playwright E2E
4. Kubernetes, Kong e KEDA (a API roda com docker-compose)
5. Busca (006) e recibo (007)
6. Frontend Blazor (a API é demonstrada pelo Swagger)
7. Features 004 e 008 (mantendo 001, 002, 003 e 005 como API testada)
