# Roteiro do vídeo — Conexão Solidária

[← README](../README.md)

Vídeo de entrega da **Atividade Substitutiva da Fase 3**. O enunciado pede um vídeo "demonstrando todos os passos expostos funcionando corretamente" e avalia **o fluxo de CI com a cobertura do código**. Não há duração máxima definida; o roteiro abaixo tem cerca de 10 minutos para manter o foco no que é avaliado.

Passos que precisam aparecer (enunciado): API .NET escolhida → testes unitários (sucesso e erro, mais de um teste) → pipeline de CI executando os testes → cobertura retornada pela pipeline, com download.

## Preparação (antes de gravar)

- [ ] `main` com a pipeline verde e pelo menos uma execução concluída (plano B).
- [ ] Clone limpo aberto no VS Code; `dotnet build` e `dotnet test --filter Category=Unit` já executados uma vez (para não esperar o restore na gravação).
- [ ] Um commit pequeno preparado para disparar a pipeline ao vivo (ex.: texto no README), ou usar o botão **Run workflow**.
- [ ] Navegador logado no GitHub, na aba **Actions** do repositório.
- [ ] API no ar com docker-compose e Swagger aberto (para mostrar a API funcionando).
- [ ] Notificações desligadas; fonte do terminal e do navegador ampliada; nenhum segredo visível (`.env`, tokens).
- [ ] OBS: 1080p, áudio testado, cenas "Tela cheia" e "Câmera + tela".
- [ ] Ensaio cronometrado feito pelo menos uma vez.

## Roteiro

| Tempo | Bloco | Tela | O que mostrar e falar |
|---|---|---|---|
| 00:00–00:30 | 1. Abertura | Câmera + slide de título | Integrante(s), trabalho (Atividade Substitutiva da Fase 3) e objetivo: assegurar a qualidade de uma API .NET com testes unitários e CI com cobertura. |
| 00:30–02:00 | 2. A API escolhida | README, `specs/product/overview.md`, Swagger | Conexão Solidária: plataforma de doações. Funcionalidades testáveis: campanhas, doadores, doações. Mostrar rapidamente as camadas (domínio, casos de uso, endpoints) e uma chamada no Swagger (ex.: criar campanha válida → 201 e meta 0 → 422). |
| 02:00–04:30 | 3. Testes unitários | VS Code e terminal | Estrutura de `tests/unit/` e o framework (xUnit). Abrir 2 ou 3 classes de teste mostrando **caso de sucesso** e **cenário de erro** lado a lado (ex.: `Criar_CampanhaValida_RetornaAtiva` e `Criar_MetaZero_RetornaErroMetaInvalida`; `Cpf` válido e inválido). Rodar `dotnet test --filter Category=Unit` e mostrar a contagem de testes aprovados. |
| 04:30–06:30 | 4. Pipeline de CI | GitHub (`ci.yml` e aba Actions) | Abrir `.github/workflows/ci.yml`: gatilhos (`push`, `pull_request`, `workflow_dispatch`) e os passos do job `testes-unitarios` (restore, build, testes com coleta de cobertura, relatório). Disparar a pipeline (push do commit preparado ou **Run workflow**) e acompanhar os passos ficando verdes. |
| 06:30–08:30 | 5. Cobertura na pipeline | GitHub (resumo e artefatos) | Na página da execução: **resumo de cobertura** (linhas e ramos por projeto). Em **Artifacts**: baixar `cobertura-testes-unitarios`, abrir o `index.html` e navegar até uma classe mostrando linhas cobertas e não cobertas. Mostrar também o artefato de resultados (`.trx`). |
| 08:30–09:30 | 6. Pipeline barrando falha (opcional) | GitHub | Execução numa branch com um teste quebrado: job vermelho, teste que falhou identificado. Explicar que a `main` só aceita merge com a pipeline verde. |
| 09:30–10:00 | 7. Encerramento | Câmera + README | Recapitular: API escolhida, testes de sucesso e erro, pipeline executando e cobertura disponível para download. Link do repositório. |

## Plano B

| Falha durante a gravação | Ação |
|---|---|
| Pipeline demora | Mostrar a execução já concluída e seguir; voltar ao final se terminar |
| Artefato expirado | Rodar a pipeline pelo **Run workflow** antes de gravar o bloco 5 |
| Docker indisponível na máquina | Pular a chamada no Swagger; os testes unitários não dependem de Docker |
| Teste falhando sem querer | Mostrar o erro como exemplo do bloco 6 e corrigir depois da gravação |

## Pós-gravação

- [ ] Conferir se todos os passos do enunciado aparecem: API, testes de sucesso e erro, mais de um teste, pipeline executando, cobertura com download.
- [ ] Conferir o áudio e se nenhum segredo apareceu.
- [ ] Publicar o vídeo (YouTube não listado ou Drive com acesso por link) e testar o link em aba anônima.
- [ ] Colocar o link no README e enviar na plataforma da FIAP.
