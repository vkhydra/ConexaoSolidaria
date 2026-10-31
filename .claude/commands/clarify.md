---
description: Encontra ambiguidades na spec da feature atual, pergunta ao usuário (até 5 perguntas) e registra as respostas na spec
argument-hint: "[NNN da feature; padrão: a da branch atual]"
---

# /clarify

Esclareça a spec da feature **$ARGUMENTS** (se vazio, use o número da branch atual `feat/NNN-*`).

## Passos

1. Localize `specs/features/NNN-*/spec.md`. Se não existir, oriente a rodar `/specify` e pare.
2. Leia a spec, `specs/constitution.md`, `docs/glossary.md` e as specs das features de que ela depende.
3. Procure lacunas nestas categorias e marque cada uma como clara, parcial ou ausente:
   - escopo e fora do escopo;
   - atores e permissões (401/403);
   - dados, validações e limites;
   - fluxos de erro (400, 404, 409, 422, 503);
   - estados e transições;
   - consistência eventual e eventos;
   - segurança e LGPD;
   - desempenho e volume (metas mensuráveis);
   - integração com outras features;
   - termos fora do glossário.
4. Monte no máximo **5 perguntas**, priorizando as que mudam arquitetura, dados ou testes. Inclua os `[PRECISA ESCLARECER]` existentes.
5. Faça **uma pergunta por vez** com a ferramenta de perguntas, com 2 a 4 opções e a recomendada primeiro, marcada como "(Recomendado)".
6. Após cada resposta:
   - registre em `## Esclarecimentos` → `### Sessão AAAA-MM-DD` como `- **P:** ... → **R:** ...`;
   - atualize a seção afetada (FR, cenário, caso de borda, critério) para refletir a resposta;
   - remova o `[PRECISA ESCLARECER]` correspondente.
7. Pare antes de 5 perguntas se não houver mais lacunas relevantes.
8. Commit com a skill `commit-gitmoji`: `📝 docs(specs): registra esclarecimentos da feature NNN`.

## Saída

Quantidade de perguntas feitas, seções alteradas, categorias que ficaram parciais (com o motivo) e o próximo passo (`/plan`).

## Regras

- Não invente respostas: se o usuário não responder, mantenha a pendência.
- Não introduza detalhes de implementação na spec.
