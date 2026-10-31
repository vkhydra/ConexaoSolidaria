---
description: Analisa (somente leitura) a consistência entre spec, plano, tarefas e constituição da feature e gera um relatório de achados
argument-hint: "[NNN da feature; padrão: a da branch atual]"
---

# /analyze

Analise a feature **$ARGUMENTS** (se vazio, use a branch atual `feat/NNN-*`). **Não altere nenhum arquivo.**

## Entradas

`spec.md`, `research.md`, `plan.md`, `data-model.md`, `contracts/*`, `tasks.md` da feature, `specs/constitution.md`, `docs/glossary.md` e os documentos de `specs/product/` citados no plano.

## Verificações

| # | Verificação | Severidade se falhar |
|---|---|---|
| 1 | Princípio da constituição violado sem justificativa | CRÍTICO |
| 2 | FR sem tarefa correspondente | CRÍTICO |
| 3 | Cenário Gherkin sem tarefa de automação | ALTO |
| 4 | Tarefa sem FR, história ou motivo técnico rastreável | MÉDIO |
| 5 | Endpoint ou evento do contrato diferente do modelo de dados ou da spec (nomes, campos, códigos de erro) | ALTO |
| 6 | Código de erro usado na spec e ausente no contrato (ou o contrário) | MÉDIO |
| 7 | Termo fora do glossário ou usado com sentido diferente | BAIXO |
| 8 | Requisito vago ou não mensurável ("rápido", "seguro") | MÉDIO |
| 9 | Duplicidade ou conflito com outra feature ou com `specs/product/` | ALTO |
| 10 | Mensagem de commit fora do padrão ou tarefa que não compila sozinha | MÉDIO |
| 11 | Falta de tarefa de cobertura, `<summary>`, observabilidade ou segurança exigida | ALTO |
| 12 | `[PRECISA ESCLARECER]` restante | CRÍTICO |

## Saída

```markdown
## Relatório de análise — Feature NNN

| ID | Severidade | Verificação | Onde | Achado | Sugestão |
|---|---|---|---|---|---|

### Cobertura
| FR | Tarefas | Cenários |

### Métricas
- FRs: n · cobertos por tarefa: n (%)
- Cenários: n · automatizados: n (%)
- Achados: CRÍTICO n · ALTO n · MÉDIO n · BAIXO n
```

No máximo 50 achados, do mais grave para o menos grave.

## Depois do relatório

- Com CRÍTICO: recomende não rodar `/implement` até corrigir.
- Pergunte se o usuário quer que você aplique as correções sugeridas; só edite com a confirmação, num passo separado.
