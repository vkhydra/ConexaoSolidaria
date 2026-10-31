---
description: Gera o plano técnico da feature (research.md, data-model.md, contracts/ e plan.md) verificando a constituição
argument-hint: "[NNN da feature e orientações técnicas opcionais]"
---

# /plan

Planeje a implementação da feature **$ARGUMENTS** (se não houver número, use o da branch atual `feat/NNN-*`).

## Pré-condições

- `spec.md` existe e não tem `[PRECISA ESCLARECER]`. Se tiver, oriente a rodar `/clarify` e pare.
- Pode delegar a análise ao subagente `architect`.

## Fase 0 — Pesquisa (`research.md`)

1. Leia a spec, `specs/constitution.md` e os documentos de `specs/product/` (arquitetura, dados, API e eventos, segurança, observabilidade, infraestrutura).
2. Liste as incertezas técnicas (biblioteca, padrão, limite, compatibilidade).
3. Pesquise cada uma, nesta ordem: ADRs em `docs/adr/`; LLMWiki (somente leitura, `wiki:...`); MCP Microsoft Learn para APIs .NET/Azure atuais.
4. Registre cada item como `## Rn — <tema>` com **Decisão**, **Justificativa**, **Alternativas** e **Fonte**.
5. Decisão arquitetural nova (afeta mais de uma feature ou muda a stack): crie o ADR com a skill `nova-adr` e cite-o.

## Fase 1 — Design

1. `data-model.md`: agregados, value objects, regras com códigos de erro, estados (diagrama Mermaid `stateDiagram-v2`), tabelas/coleções/índices usados.
2. `contracts/api.md`: cada endpoint com exemplo JSON de requisição e resposta e a tabela "Status | Código | Quando".
3. `contracts/eventos.md` (se a feature publica ou consome eventos): record C# com `/// <summary>`, exchange, fila, DLQ e regras de versão.
4. `plan.md` com o modelo abaixo.

## Modelo de `plan.md`

```markdown
# Plano técnico — Feature NNN (<Título>)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato](contracts/api.md) · [Tarefas](tasks.md)

## Resumo
## Contexto técnico
| Item | Valor |   (dependências, armazenamento, testes, metas, restrições)
## Verificação da constituição
| Princípio | Atendido | Como |   (I a XII; ✅, N/A ou ❌ com justificativa)
## Estrutura de código
## Design
## Observabilidade
## Riscos
```

## Portão da constituição

- Preencha a tabela com **todos** os princípios de `specs/constitution.md`.
- Se algum ficar ❌ sem justificativa aceitável, **pare**, explique o conflito e proponha alternativas.
- Exceção justificada fica registrada na própria tabela e precisa de aprovação no PR.

## Commits

Um commit por arquivo, com a skill `commit-gitmoji`, nesta ordem: `research.md`, `data-model.md`, `contracts/*`, `plan.md`.

## Saída

Arquivos criados, decisões principais, ADRs criados, resultado do portão da constituição e o próximo passo (`/tasks`).
