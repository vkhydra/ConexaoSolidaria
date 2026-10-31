# Modelo de dados — Feature 009 (Testes unitários e cobertura no CI)

[← Spec](spec.md)

Esta feature não tem tabelas: os "dados" são os arquivos produzidos pela pipeline.

## Artefatos da execução

| Artefato | Job | Conteúdo | Retenção |
|---|---|---|---|
| `cobertura-testes-unitarios` | `testes-unitarios` | `index.html` (relatório navegável), `Cobertura.xml`, `Summary.json`, `SummaryGithub.md` | 14 dias |
| `resultados-testes-unitarios` | `testes-unitarios` | Arquivos `.trx` de cada projeto de teste | 14 dias |
| `cobertura-total` | `testes-integracao` | Mesmos formatos, somando unidade, contrato, integração e aceitação | 14 dias |
| `resultados-testes` | `testes-integracao` | Arquivos `.trx` de todos os projetos | 14 dias |

## `Summary.json` (campos usados)

| Campo | Significado |
|---|---|
| `summary.linecoverage` | Cobertura de linhas total (%) |
| `summary.branchcoverage` | Cobertura de ramos total (%) |
| `summary.coveredlines`, `summary.coverablelines` | Linhas cobertas e cobríveis |
| `coverage.assemblies[].name` | Projeto medido (ex.: `Campanhas.Domain`) |
| `coverage.assemblies[].coverage` | Cobertura de linhas do projeto (%) |
| `coverage.assemblies[].branchcoverage` | Cobertura de ramos do projeto (%) |

## Resumo exibido na página da execução

```markdown
## Cobertura — testes unitários
| Projeto | Linhas | Ramos |
|---|---|---|
| Campanhas.Domain | 97,4% | 95,0% |
| Identity.Domain | 96,1% | 93,8% |
| **Total** | **94,2%** | **91,5%** |
```

Valores ilustrativos.
