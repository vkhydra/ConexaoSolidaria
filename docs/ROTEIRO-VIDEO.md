# Roteiro do vídeo — Conexão Solidária

[← README](../README.md)

Vídeo de entrega da **Atividade Substitutiva da Fase 3**. O enunciado pede um vídeo "demonstrando todos os passos expostos funcionando corretamente" e avalia **o fluxo de CI com a cobertura do código**. Não há duração máxima definida; o roteiro abaixo tem cerca de 9 a 10 minutos para manter o foco no que é avaliado.

Passos que precisam aparecer (enunciado): API .NET escolhida → testes unitários (sucesso e erro, mais de um teste) → pipeline de CI executando os testes → cobertura retornada pela pipeline, com download.

## Preparação (antes de gravar)

- [ ] `main` com a pipeline verde e pelo menos uma execução concluída (plano B). O workflow dispara em `push` na `main`, em `pull_request` para a `main` e em **Run workflow**; confira o resultado em `https://github.com/Kulessa/Trabalho/actions`.
- [ ] Clone limpo aberto no VS Code; `dotnet build` e `dotnet test --filter Category=Unit` já executados uma vez (para não esperar o restore na gravação).
- [ ] Para o bloco 4, basta o botão **Run workflow** (ou o último push na `main`). O teste quebrado com PR (R046) foi dispensado e não entra no vídeo.
- [ ] Não afirme no vídeo que a `main` é protegida ou que o merge exige a pipeline verde: isso não foi verificado.
- [ ] Navegador logado no GitHub, na aba **Actions** do repositório.
- [ ] Parar o que não for usar e subir a stack: `pwsh ./scripts/gerar-env.ps1`, `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build` e `pwsh ./scripts/seed-demo.ps1`. Abrir a coleção Postman ([docs/postman](postman/README.md)) e a interface Web em `http://localhost:8080`. A API não tem Swagger. Reserve ~4 GB de memória para o Docker e **não rode os testes de integração com a stack de pé**.
- [ ] Notificações desligadas; fonte do terminal e do navegador ampliada; nenhum segredo visível (`.env`, tokens).
- [ ] OBS: 1080p, áudio testado, cenas "Tela cheia" e "Câmera + tela".
- [ ] Ensaio cronometrado feito pelo menos uma vez.

## Roteiro

| Tempo | Bloco | Tela | O que mostrar e falar |
|---|---|---|---|
| 00:00–00:30 | 1. Abertura | Câmera + slide de título | Integrante(s), trabalho (Atividade Substitutiva da Fase 3) e objetivo: assegurar a qualidade de uma API .NET com testes unitários e CI com cobertura. |
| 00:30–02:00 | 2. A API escolhida | README, `specs/product/overview.md`, Swagger | Conexão Solidária: plataforma de doações. Funcionalidades testáveis: campanhas, doadores, doações. Mostrar rapidamente as camadas (domínio, casos de uso, endpoints) e duas chamadas na coleção Postman: criar campanha válida → 201 e meta 0 → 422 (`campanha.meta_invalida`); em seguida, a doação aparecendo no painel de transparência. |
| 02:00–04:30 | 3. Testes unitários | VS Code e terminal | Estrutura de `tests/unit/` e o framework (xUnit). Abrir 2 ou 3 classes de teste mostrando **caso de sucesso** e **cenário de erro** lado a lado (arquivos reais: `tests/unit/Campanhas.UnitTests/Campanhas/CampanhaCriarTests.cs` com `Criar_Meta_AceitaSoDentroDoIntervalo`; `tests/unit/Identity.UnitTests/Usuarios/CpfTests.cs` com `Criar_CpfValido_GuardaSoDigitos` e `Criar_CpfInvalido_DevolveErro`; `tests/unit/Identity.UnitTests/Doadores/CadastrarDoadorTests.cs` com `ExecutarAsync_DadosValidos_GravaDoadorEDevolveCpfMascarado` e `ExecutarAsync_CpfEmUso_DevolveConflito`). Rodar `dotnet test ConexaoSolidaria.slnx -c Release --filter Category=Unit` e mostrar a contagem de testes aprovados (confira o total no dia: eram 719 nos projetos de unidade). |
| 04:30–06:30 | 4. Pipeline de CI | GitHub (`ci.yml` e aba Actions) | Abrir `.github/workflows/ci.yml`: gatilhos (`push`, `pull_request`, `workflow_dispatch`) e os passos do job `testes-unitarios` (restore, formato, build, testes com coleta de cobertura, relatório, artefatos). Citar que o job `testes-integracao` roda todos os testes e exige 90% de cobertura. Disparar a pipeline (pull request da branch preparada ou **Run workflow**) e acompanhar os passos ficando verdes. |
| 06:30–08:30 | 5. Cobertura na pipeline | GitHub (resumo e artefatos) | Na página da execução: **resumo de cobertura** (linhas e ramos por projeto). Em **Artifacts**: baixar `cobertura-testes-unitarios`, abrir o `index.html` e navegar até uma classe mostrando linhas cobertas e não cobertas. Mostrar também o artefato de resultados (`.trx`). |
| 08:30–09:30 | 6. Encerramento | Câmera + README | Recapitular: API escolhida, testes de sucesso e erro, pipeline executando e cobertura disponível para download. Link do repositório. |

## Plano B

| Falha durante a gravação | Ação |
|---|---|
| Pipeline demora | Mostrar a execução já concluída e seguir; voltar ao final se terminar |
| Artefato expirado | Rodar a pipeline pelo **Run workflow** antes de gravar o bloco 5 |
| Docker indisponível na máquina | Pular a demonstração da API (Postman e Web); os testes unitários não dependem de Docker |
| Docker travado ou sem memória | Parar a stack (`docker compose ... stop`) antes de rodar testes; reiniciar o Docker Desktop só se necessário |
| Teste falhando sem querer | Mostrar o erro, explicar que a pipeline o aponta e corrigir depois da gravação |

## Pós-gravação

- [ ] Conferir se todos os passos do enunciado aparecem: API, testes de sucesso e erro, mais de um teste, pipeline executando, cobertura com download.
- [ ] Conferir o áudio e se nenhum segredo apareceu.
- [ ] Publicar o vídeo (YouTube não listado ou Drive com acesso por link) e testar o link em aba anônima.
- [ ] Colocar o link no README e enviar na plataforma da FIAP.
