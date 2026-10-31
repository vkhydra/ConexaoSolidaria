# ADR 0016 — Frontend Blazor Web App

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto
- **Errata (2026-10-30):** contexto corrigido para o escopo do trabalho substitutivo ([ADR 0018](0018-escopo-trabalho-substitutivo.md)); a decisão não mudou.

## Contexto

A Fase 5 ensina Blazor e Razor. A API pode ser usada por Postman ou Swagger, mas um frontend aplica esse conteúdo e melhora a demonstração.

## Decisão

Blazor Web App com SSR estático nas páginas públicas e `InteractiveServer` nos formulários. O JWT fica no servidor (cookie de autenticação do Blazor); nunca no `localStorage`.

## Alternativas consideradas

Blazor WebAssembly (token no navegador, maior download); SPA em JavaScript (fora do curso).

## Consequências

Circuito SignalR por usuário; afinidade de sessão no Kong para o Web com 2 réplicas.

## Referências

- [Arquitetura](../../specs/product/arquitetura.md)
- [Manual do usuário](../manual-do-usuario.md)
