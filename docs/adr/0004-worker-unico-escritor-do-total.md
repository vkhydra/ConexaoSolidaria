# ADR 0004 — Worker como único escritor do total arrecadado

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

O enunciado exige que o Worker atualize o "Valor Total Arrecadado" e que a API não o faça.

## Decisão

`Arrecadacao.Worker` pertence ao contexto Campanhas, compartilha o `campanhas_db` com a `Campanhas.Api` e é o único que escreve `total_arrecadado`. A API só lê.

## Alternativas consideradas

Worker com banco próprio e evento de volta para a API atualizar o total: a API voltaria a atualizar o total, contrariando o enunciado.

## Consequências

"Um contexto, dois deployables": escalam separadamente. O banco é do contexto, não de cada processo. As migrations ficam no `Campanhas.Infrastructure`.

## Referências

- [Feature 005 — Doação](../../specs/features/005-doacao/spec.md)
- [Dados](../../specs/product/dados.md)
