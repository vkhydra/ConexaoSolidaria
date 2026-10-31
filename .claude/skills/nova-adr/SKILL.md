---
name: nova-adr
description: Cria um Architecture Decision Record numerado em docs/adr/ no modelo do projeto e atualiza o índice. Use quando surgir uma decisão arquitetural nova ou quando uma decisão existente for substituída.
---

# Novo ADR

## Quando usar

- Escolha de tecnologia, padrão ou biblioteca que afeta mais de uma feature.
- Mudança de decisão já registrada (o ADR antigo vira `Substituído por NNNN`).
- O `/plan` encontrou uma decisão que não está em `docs/adr/`.

## Passos

1. Leia `docs/adr/README.md` (índice, regras e modelo) e os ADRs relacionados ao tema.
2. Confirme que a decisão é nova. Se já existir ADR aceito sobre o tema, trate como substituição.
3. Número: maior `NNNN` de `docs/adr/` + 1 (4 dígitos).
4. Nome do arquivo: `docs/adr/NNNN-<titulo-curto-em-kebab-case-sem-acentos>.md`.
5. Preencha o modelo:
   - **Status:** `Proposto` (vira `Aceito` na aprovação do PR);
   - **Data:** hoje (`AAAA-MM-DD`);
   - **Contexto:** problema, forças e restrições (cite RF, RNF e constituição);
   - **Decisão:** no presente ("Usar ...");
   - **Alternativas consideradas:** pelo menos duas, com o motivo da recusa;
   - **Consequências:** positivas, negativas e o que muda no projeto;
   - **Referências:** specs, features, páginas da LLMWiki (`wiki:...`) e documentação oficial.
6. Acrescente a linha no índice de `docs/adr/README.md`.
7. Substituição: no ADR antigo, altere **só** o status para `Substituído por [NNNN](NNNN-...md)`.
8. Cite o ADR no `research.md` ou `plan.md` da feature que motivou a decisão.
9. Commit com a skill `commit-gitmoji`: `📝 docs(adr): adiciona adr NNNN (<tema>)`, incluindo o índice no mesmo commit.

## Regras

- ADR aceito não é editado (exceto a troca de status na substituição).
- Linguagem objetiva, em português do Brasil, sem jargão desnecessário.
- Decisão que contraria a constituição exige antes o comando `/constitution`.
