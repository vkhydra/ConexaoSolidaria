# ADR 0013 — Bibliotecas com licença comercial evitadas

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

MediatR, AutoMapper, FluentAssertions e MassTransit v9 mudaram para licenças comerciais.

## Decisão

Casos de uso chamados diretamente (sem mediator); mapeamento manual em métodos de extensão; asserções com Shouldly; MassTransit 8.x.

## Consequências

Menos dependências e mais código explícito.

## Referências

- [Padrões de código](../padroes-de-codigo.md)
