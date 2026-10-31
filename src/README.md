# Código-fonte

Código da API **Conexão Solidária** (.NET 10), testada conforme a [feature 009](../specs/features/009-ci-testes-cobertura/spec.md). Os projetos são criados na Etapa 3 do [plano de execução](../docs/plano-de-execucao.md) e preenchidos pelas features 001 a 008.

## Estrutura

| Pasta | Projetos | Responsabilidade |
|---|---|---|
| `BuildingBlocks/` | `ConexaoSolidaria.Contracts`, `ConexaoSolidaria.ServiceDefaults` | Contratos de eventos v1 e configuração comum (logs, traces, métricas, health checks, JWT, tratamento de erros) |
| `Services/Identity/` | `Identity.Domain`, `Identity.Application`, `Identity.Infrastructure`, `Identity.Api` | Cadastro, login, JWT e direitos do titular (features 001, 003 e 008) |
| `Services/Campanhas/` | `Campanhas.Domain`, `Campanhas.Application`, `Campanhas.Infrastructure`, `Campanhas.Api`, `Arrecadacao.Worker` | Campanhas, painel, busca, doação e processamento (features 002, 004, 005 e 006) |
| `Functions/` | `Notificacoes.Function` | Recibo de doação, serverless com KEDA (feature 007) |
| `Web/` | `ConexaoSolidaria.Web` | Interface Blazor |

## Regras

- Dependências apontam para dentro: `Api`/`Worker` → `Application` → `Domain`; `Infrastructure` implementa as interfaces de `Application`. `Domain` não referencia infraestrutura (verificado por teste de arquitetura).
- Bounded contexts não se referenciam: Identity e Campanhas conversam só por JWT e eventos de `ConexaoSolidaria.Contracts`.
- Todo tipo e membro tem `/// <summary>` em português ([padrões de código](../docs/padroes-de-codigo.md)).
- Toda funcionalidade tem testes unitários de sucesso e de erro em `tests/unit/` ([testes](../docs/testes.md)).
- Cada projeto entra na `ConexaoSolidaria.slnx` no mesmo commit em que é criado.

Arquitetura completa: [specs/product/arquitetura.md](../specs/product/arquitetura.md).
