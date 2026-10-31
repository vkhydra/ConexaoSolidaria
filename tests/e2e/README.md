# Testes ponta a ponta

Validam as funcionalidades do ponto de vista do usuário.

| Pasta / projeto | Ferramenta | Cobre | Quando roda |
|---|---|---|---|
| `ConexaoSolidaria.Bdd` | Reqnroll + xUnit | Um arquivo `.feature` por feature de `specs/features/`, com os cenários Gherkin da `spec.md` | CI |
| `ConexaoSolidaria.E2ETests` | Playwright para .NET | Jornada "cadastrar → entrar → doar → ver o painel" no navegador | Manual ou agendado (bônus) |
| `load/` | k6 | `painel.js`, `doacao.js`, `busca.js` com thresholds dos requisitos não funcionais | Manual, antes do vídeo |

## Regras

- Arquivos `.feature` em português (`# language: pt`), nomeados como a feature: `005-doacao.feature`.
- Steps declarativos ("o doador doa 150.00"), sem detalhes de HTTP no texto.
- Resultados do k6 em `load/results/` (ignorado pelo git).

Estratégia completa: [docs/testes.md](../../docs/testes.md).
