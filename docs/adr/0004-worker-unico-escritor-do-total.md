# ADR 0004 — Worker como único escritor do total arrecadado

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A doação é processada de forma assíncrona (mensageria, Fase 2): a API aceita a doação e o Worker atualiza o "Valor Total Arrecadado". Dois escritores do mesmo valor gerariam conflito de concorrência e regras duplicadas.

## Decisão

`Arrecadacao.Worker` pertence ao contexto Campanhas, compartilha o `campanhas_db` com a `Campanhas.Api` e é o único que escreve `total_arrecadado`. A API só lê.

## Alternativas consideradas

Worker com banco próprio e evento de volta para a API atualizar o total: a API voltaria a atualizar o total, com dois escritores do mesmo valor.

## Consequências

"Um contexto, dois deployables": escalam separadamente. O banco é do contexto, não de cada processo. As migrations ficam no `Campanhas.Infrastructure`.

## Referências

- [Feature 005 — Doação](../../specs/features/005-doacao/spec.md)
- [Dados](../../specs/product/dados.md)
