# Definition of Ready (DoR)

[← README](../../README.md) · [Processo ágil](../processo-agil.md)

Um item só entra na coluna **Pronto (DoR)** do quadro quando todos os critérios abaixo estão atendidos. O objetivo é nunca começar uma tarefa sem saber o que entregar, como testar e do que ela depende.

## Checklist

- [ ] Está escrito como história (`Como <persona>, quero <ação>, para <benefício>`) ou como tarefa técnica com objetivo claro.
- [ ] A feature tem `spec.md` em `specs/features/NNN-nome/` sem `[PRECISA ESCLARECER]` (comandos `/specify` e `/clarify`).
- [ ] Tem critérios de aceitação testáveis em Gherkin na spec.
- [ ] `plan.md` e `tasks.md` existem e o `/analyze` não aponta achados críticos.
- [ ] Atende ao INVEST: independente, negociável, valiosa, estimável, pequena e testável.
- [ ] Está estimado (Planning Poker, escala de Fibonacci) e cabe em até 3 dias de trabalho; item de 13 pontos é dividido.
- [ ] Dependências identificadas (outras features, infraestrutura, decisões em aberto).
- [ ] Persona citada existe em [visão geral do produto](../../specs/product/overview.md#personas).

## Quem verifica

O Product Owner confere os itens de negócio (história, critérios de aceitação, persona). Quem vai implementar confere os itens técnicos (plano, tarefas, dependências, estimativa). O item só se move para **Pronto (DoR)** com os dois de acordo.
