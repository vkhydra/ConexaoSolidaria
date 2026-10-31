# Plano técnico — Feature 008 (Direitos do titular)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato](contracts/api.md) · [Tarefas](tasks.md)

## Resumo

Dois endpoints na `Identity.Api`: consulta dos próprios dados (com CPF decifrado e mascarado) e anonimização do cadastro, com auditoria em log e recusa do token antigo na Identity.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | As da [feature 003](../003-cadastro-doador/plan.md) (`ICifradorCpf`) |
| Armazenamento | PostgreSQL `identity_db` |
| Testes | xUnit v3, Testcontainers (PostgreSQL), Reqnroll, bUnit |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de acesso, anonimização e token antigo |
| II. Domínio | ✅ | `Usuario.Anonimizar` no agregado |
| III. TDD | ✅ | Anonimizar, anonimizar duas vezes, senha após anonimizar |
| IV. Cobertura | ✅ | Unidade + integração com conferência das colunas no banco |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | Policy `Doador`; `no-store`; auditoria sem dados pessoais |
| VIII. Eventos | N/A | Nenhum evento (decisão R2 da [pesquisa](research.md)) |
| IX. Observabilidade | ✅ | `titulares_anonimizados_total`; log `TitularAnonimizado` |
| X. Infra | N/A | Sem objeto novo |
| XI. Simplicidade | ✅ | Sem lista de revogação de tokens |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/Services/Identity/
├── Identity.Domain/Usuarios/Usuario.cs                       # Anonimizar
├── Identity.Application/Titular/{ObterMeusDados.cs, AnonimizarMeusDados.cs, DoadorResponse.cs}
└── Identity.Api/Endpoints/TitularEndpoints.cs
tests/unit/Identity.UnitTests/Usuarios/UsuarioAnonimizacaoTests.cs
tests/integration/Identity.IntegrationTests/TitularTests.cs
tests/e2e/ConexaoSolidaria.Bdd/Features/008-direitos-titular.feature
```

## Design

1. `GET /doadores/me`: lê o `sub`, carrega o usuário; anonimizado → 401; decifra o CPF e devolve mascarado.
2. `DELETE /doadores/me`: carrega, chama `Anonimizar(agora)`, salva na transação, registra `TitularAnonimizado`, responde 204.

## Observabilidade

| Sinal | Definição |
|---|---|
| Métrica | `titulares_anonimizados_total` |
| Log | `TitularAnonimizado` com `UsuarioId` |
| Trace | Spans `ObterMeusDados` e `AnonimizarMeusDados` sem atributos pessoais |

## Riscos

| Risco | Mitigação |
|---|---|
| Token ainda válido em outros serviços por até 60 min | Risco aceito e documentado em [segurança e LGPD](../../product/seguranca-e-lgpd.md) |
| Backup com dados antigos | Backups locais apagados após a demonstração ([dados](../../product/dados.md)) |
