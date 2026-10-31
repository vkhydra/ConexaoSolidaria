# ADR 0018 — Escopo: trabalho substitutivo da Fase 3

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

A primeira versão da especificação usou como fonte de requisitos o enunciado de outro trabalho do curso. O projeto, na verdade, é a **Atividade Substitutiva da Fase 3** ("Arquitetura de Sistema .NET com Azure"), cujo enunciado pede:

1. escolher uma API .NET com funcionalidades testáveis (métodos, serviços ou controladores);
2. implementar **testes unitários** com o framework de testes do .NET (MSTest, NUnit ou xUnit), cobrindo **casos de sucesso e cenários de erro**;
3. configurar uma **pipeline de CI** (GitHub Actions ou Azure DevOps) que **rode os testes unitários e retorne a cobertura**, pelo Sonar ou por uma configuração simples com os dados de cobertura **para download na pipeline**;
4. entregar **um vídeo** mostrando todos os passos funcionando, com **mais de um teste** (não precisa de 100% de cobertura).

O grupo quer, além disso, aplicar o conteúdo das 5 fases do curso no projeto.

## Decisão

1. Os requisitos **avaliados** são os do enunciado do substitutivo, especificados na [feature 009](../../specs/features/009-ci-testes-cobertura/spec.md).
2. A API escolhida é a plataforma de doações **Conexão Solidária** (domínio mantido). Suas funcionalidades (features 001 a 008) são o conteúdo testado; elas e a arquitetura aplicam o conteúdo das 5 fases por escolha do projeto, não por exigência de enunciado.
3. Enunciados de outros trabalhos do curso não são fonte de requisitos nem de entregáveis.
4. Framework de testes: **xUnit** (v3).
5. Pipeline: **GitHub Actions**; cobertura com coverlet + ReportGenerator, publicada como **artefato para download** e como resumo na página da execução.
6. Entregável: um vídeo ([roteiro](../ROTEIRO-VIDEO.md)).

## Alternativas consideradas

- **MSTest ou NUnit:** atendem o enunciado; o xUnit já é o padrão do projeto e tem suporte nativo do `dotnet test` e do Testcontainers.
- **Azure DevOps:** atende o enunciado; o repositório já está no GitHub, e o GitHub Actions dispensa projeto separado.
- **SonarCloud:** atende o enunciado, mas exige conta e token externos; fica como evolução opcional. O artefato basta para o critério de avaliação.
- **Escopo mínimo (só uma API e testes):** atende o enunciado, mas não aplica o conteúdo das fases que o grupo quer praticar.

## Consequências

- A feature 009 é a prioridade máxima: a pipeline com cobertura sai na Etapa 3 do plano e é demonstrada no vídeo.
- O que vai além do enunciado (Kubernetes, gateway, observabilidade, serverless) continua especificado como aplicação das fases e pode ser cortado sem afetar a avaliação ([entrega e riscos](../entrega-e-riscos.md)).
- Documentos que citavam outro enunciado como origem foram corrigidos.

## Referências

- Enunciado: `C:\Victor\Fiap\fiap-pdfs\Trabalho Substitutivo\fase 3.pdf`
- `wiki:fontes/tech-challenge/06-trabalho-substitutivo-fase-3-testes-e-cobertura-ci`
- `wiki:conceitos/cobertura-de-codigo`, `wiki:conceitos/teste-de-unidade`, `wiki:conceitos/github-actions`
