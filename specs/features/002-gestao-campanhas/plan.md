# Plano técnico — Feature 002 (Gestão de campanhas)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato HTTP](contracts/api.md) · [Eventos](contracts/eventos.md) · [Tarefas](tasks.md)

## Resumo

CRUD de campanhas na `Campanhas.Api` com o agregado `Campanha` fazendo todas as regras, concorrência otimista por `xmin`/`ETag` e publicação de `CampanhaAlteradaEvent` pelo Outbox do MassTransit.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `MassTransit.RabbitMQ`, `MassTransit.EntityFrameworkCore` (8.x), `FluentValidation` |
| Armazenamento | PostgreSQL `campanhas_db` |
| Testes | xUnit v3, `FakeTimeProvider`, Testcontainers (PostgreSQL, RabbitMQ), MassTransit Test Harness, Reqnroll, bUnit |
| Metas | Criação p95 < 300 ms; evento no Outbox em 100% das alterações |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários Gherkin e esclarecimento sobre data início futura |
| II. Domínio | ✅ | Regras em `Campanha`, `Periodo`, `Dinheiro`; erros como `Result` |
| III. TDD | ✅ | Agregado e VOs com testes antes do código |
| IV. Cobertura | ✅ | Unidade (domínio, casos de uso) + integração (endpoints, concorrência, Outbox) |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | Policy `GestorONG`; sem dados pessoais |
| VIII. Eventos | ✅ | Outbox na mesma transação; contrato v1 com teste de snapshot |
| IX. Observabilidade | ✅ | `campanhas_criadas_total`; span por caso de uso |
| X. Infra | N/A | Sem objeto novo de infraestrutura |
| XI. Simplicidade | ✅ | Casos de uso chamados direto, sem mediator |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/BuildingBlocks/ConexaoSolidaria.Contracts/V1/CampanhaAlteradaEvent.cs
src/Services/Campanhas/
├── Campanhas.Domain/
│   ├── Comum/{Dinheiro.cs, Periodo.cs, Result.cs, Erro.cs}
│   └── Campanhas/{Campanha.cs, CampanhaId.cs, StatusCampanha.cs, Eventos/*}
├── Campanhas.Application/
│   ├── Abstracoes/{ICampanhaRepositorio.cs, IRelogio.cs}
│   └── Campanhas/{CriarCampanha.cs, AtualizarCampanha.cs, AlterarStatusCampanha.cs, ListarCampanhas.cs, ObterCampanha.cs, Dtos/*, Validadores/*}
├── Campanhas.Infrastructure/
│   ├── Persistencia/{CampanhasDbContext.cs, Configuracoes/CampanhaConfiguration.cs, Migrations/*}
│   └── Mensageria/{ConfiguracaoMassTransit.cs, PublicadorEventosCampanha.cs}
└── Campanhas.Api/Endpoints/CampanhasEndpoints.cs
tests/unit/Campanhas.UnitTests/…
tests/integration/Campanhas.IntegrationTests/CampanhasEndpointsTests.cs
tests/contract/ConexaoSolidaria.ContractTests/CampanhaAlteradaEventTests.cs
tests/e2e/ConexaoSolidaria.Bdd/Features/002-gestao-campanhas.feature
```

## Design

1. Endpoint valida o formato (FluentValidation) → 400.
2. Caso de uso obtém "hoje" do `IRelogio` (`TimeProvider` + fuso de São Paulo).
3. O agregado aplica as regras e registra o evento de domínio.
4. `SaveChangesAsync` grava a campanha e o `CampanhaAlteradaEvent` no Outbox na mesma transação.
5. `PUT` compara `If-Match` com `xmin`; conflito → `DbUpdateConcurrencyException` → 409.

## Observabilidade

| Sinal | Definição |
|---|---|
| Métrica | `campanhas_criadas_total` |
| Log | `Information` "Campanha {CampanhaId} criada" / "alterada para {Status}" |
| Trace | Spans `CriarCampanha`, `AtualizarCampanha`, `AlterarStatusCampanha` |

## Riscos

| Risco | Mitigação |
|---|---|
| Fuso horário diferente entre máquinas | Sempre `America/Sao_Paulo` via `IRelogio`; testes com `FakeTimeProvider` |
| Evento grande no Outbox | Descrição limitada a 2000 caracteres |
