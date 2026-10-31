---
name: verificar-cobertura
description: Roda todos os testes com cobertura, gera o relatório e confere o mínimo de 90% de linhas e ramos no total e por projeto, listando o que falta cobrir. Use ao fim de cada fase do /implement, antes de PR e quando a cobertura cair.
---

# Verificar cobertura (mínimo 90%)

Regra da constituição (princípio IV): **≥ 90% de linhas e ≥ 90% de ramos**, no total e em cada projeto de `src/`. Detalhes em `docs/testes.md`.

## Passos

1. Docker ligado (testes de integração usam Testcontainers).
2. Se o script existir, use-o:
   ```bash
   pwsh ./scripts/verificar-cobertura.ps1 -Minimo 90 -ExecutarTestes
   ```
   Se ainda não existir, rode os comandos equivalentes:
   ```bash
   dotnet test -c Release --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
   dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;JsonSummary;MarkdownSummaryGithub"
   ```
3. Leia `coverage/Summary.json`:
   - total: `summary.linecoverage` e `summary.branchcoverage`;
   - por projeto: `coverage.assemblies[].name`, `coverage` (linhas) e `branchcoverage` (ramos);
   - classes abaixo da meta: `coverage.assemblies[].classesinassembly[]`.
4. Monte a tabela:

   | Projeto | Linhas | Ramos | Situação |
   |---|---|---|---|
   | Campanhas.Domain | 97,1% | 94,0% | ✅ |
   | Campanhas.Api | 86,3% | 81,0% | ❌ |

5. Para cada projeto abaixo da meta, liste as classes e os métodos com menos cobertura (abra `coverage/index.html` ou o HTML da classe) e diga **qual comportamento** falta testar: regra de validação, código de erro do contrato, ramo de falha, caso de borda da spec.
6. Se o usuário pedir, delegue a escrita dos testes ao subagente `test-writer`.

## Regras

- Não use `[ExcludeFromCodeCoverage]` para atingir a meta; só com `Justification` aprovada no PR.
- Não escreva testes sem asserção, só para passar pelas linhas.
- Exclusões válidas ficam em `coverlet.runsettings` (migrations, `Program.cs`, código gerado).
- `coverage/` e `TestResults/` não vão para o git.

## Saída

Tabela por projeto, total, veredito (✅ dentro da meta / ❌ abaixo) e a lista do que testar.
