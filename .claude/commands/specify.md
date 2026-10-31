---
description: Cria a especificação (spec.md) de uma nova feature em specs/features/NNN-nome, com branch própria
argument-hint: "<descrição da funcionalidade em linguagem natural>"
---

# /specify

Crie a especificação da funcionalidade descrita em: **$ARGUMENTS**

A spec diz **o quê** e **por quê**. Nada de classes, pacotes ou tabelas aqui; isso vai no `/plan`. Contratos HTTP (rota, status, código de erro) podem aparecer, porque são a interface do produto.

## Passos

1. Se `$ARGUMENTS` estiver vazio, peça a descrição e pare.
2. Leia `specs/constitution.md`, `specs/product/overview.md`, `docs/glossary.md` e as specs existentes em `specs/features/` para não duplicar funcionalidade.
3. Número: maior `NNN` de `specs/features/` + 1, com 3 dígitos.
4. Nome curto: 2 a 4 palavras em `kebab-case`, sem acentos (ex.: `doacao-recorrente`).
5. Branch: a partir da `main` atualizada, `git switch -c feat/NNN-nome`. Se houver mudanças não commitadas, pare e avise.
6. Crie `specs/features/NNN-nome/spec.md` com o modelo abaixo, usando os termos do glossário.
7. Para cada ponto que não dá para decidir com segurança, escreva `[PRECISA ESCLARECER: pergunta]` (no máximo 3; para o resto, use o padrão mais razoável e registre como premissa).
8. Valide com o checklist abaixo e corrija o que falhar.
9. Se surgir termo novo de negócio, acrescente em `docs/glossary.md`.
10. Commit com a skill `commit-gitmoji`: `📝 docs(specs): adiciona spec da feature NNN <nome>`.

## Modelo de `spec.md`

````markdown
# Feature NNN — <Título>

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/NNN-nome` |
| Status | Rascunho |
| Origem | <enunciado, RF, fase do curso ou pedido> |
| Histórias | <Hnn> |
| Prioridade | P1 / P2 / P3 |
| Serviço | <serviço responsável> |
| Acesso | <Público / Doador / GestorONG> |
| Depende de | <features ou "nenhuma"> |

## Objetivo
<problema do usuário e valor entregue, 2 a 4 linhas>

## Histórias de usuário
> **Hnn** — Como <persona>, quero <ação>, para <benefício>.

## Cenários de aceitação
```gherkin
# language: pt
Funcionalidade: <nome>
  Cenário: <caminho feliz>
    Dado ...
    Quando ...
    Então ...
```

## Requisitos funcionais
| ID | Requisito |
|---|---|
| FR-001 | O sistema DEVE ... |

## Casos de borda
- ...

## Entidades-chave
- **<Entidade>:** <o que representa, sem detalhes técnicos>

## Critérios de sucesso
| ID | Critério mensurável |
|---|---|
| SC-001 | ... |

## Fora do escopo
...

## Esclarecimentos
<preenchido por /clarify>
````

## Checklist da spec

- [ ] Cada FR é testável e tem pelo menos um cenário Gherkin que o cobre.
- [ ] Caminho feliz, erros de validação (400), regras de negócio (422), autorização (401/403) e casos de borda presentes.
- [ ] Critérios de sucesso mensuráveis (número, tempo, porcentagem).
- [ ] Nenhum dado pessoal desnecessário (constituição, princípio VII).
- [ ] No máximo 3 `[PRECISA ESCLARECER]`.
- [ ] Termos iguais aos do glossário.

## Saída

Caminho da spec, branch criada, lista de `[PRECISA ESCLARECER]` e próximo passo (`/clarify` se houver pendências; senão `/plan`).
