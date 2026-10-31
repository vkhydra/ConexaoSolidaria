# Plano técnico — Feature 003 (Cadastro de doador)

[← Spec](spec.md) · [Pesquisa](research.md) · [Modelo de dados](data-model.md) · [Contrato](contracts/api.md) · [Tarefas](tasks.md)

## Resumo

Endpoint público `POST /doadores` na `Identity.Api`. Os value objects `Cpf`, `Email`, `Senha` e `Consentimento` fazem as regras; o `CifradorCpf` (AES-GCM + HMAC) protege o CPF; a unicidade é garantida por consulta prévia e por constraint no banco.

## Contexto técnico

| Item | Valor |
|---|---|
| Dependências | `BCrypt.Net-Next`, `FluentValidation`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Armazenamento | PostgreSQL `identity_db`, tabela `usuarios` |
| Testes | xUnit v3, Testcontainers (PostgreSQL), Reqnroll, bUnit |
| Metas | p95 < 500 ms; nenhum dado pessoal em logs |

## Verificação da constituição

| Princípio | Atendido | Como |
|---|---|---|
| I. Spec | ✅ | Cenários de CPF, e-mail, senha e consentimento |
| II. Domínio | ✅ | VOs puros; cifragem fica na infraestrutura atrás de `ICifradorCpf` |
| III. TDD | ✅ | `Cpf`, `Senha`, `Email`, `Consentimento`, `Usuario.CadastrarDoador` |
| IV. Cobertura | ✅ | Unidade + integração com banco real |
| V. `<summary>` | ✅ | Todos os tipos e membros |
| VI. Commits | ✅ | Ver [tarefas](tasks.md) |
| VII. Segurança | ✅ | BCrypt, AES-GCM, HMAC, mensagens sem eco de valores, rate limit no Kong |
| VIII. Eventos | N/A | Nenhum evento |
| IX. Observabilidade | ✅ | `doadores_cadastrados_total`; log só com `UsuarioId` |
| X. Infra | ✅ | Chaves em Secret |
| XI. Simplicidade | ✅ | Só BCL para criptografia |
| XII. Português | ✅ | Linguagem ubíqua |

## Estrutura de código

```text
src/Services/Identity/
├── Identity.Domain/Usuarios/{Cpf.cs, Senha.cs, Consentimento.cs, Usuario.cs}
├── Identity.Application/
│   ├── Abstracoes/ICifradorCpf.cs
│   └── Doadores/{CadastrarDoador.cs, CadastrarDoadorRequest.cs, DoadorResponse.cs, CadastrarDoadorValidator.cs}
├── Identity.Infrastructure/
│   ├── Seguranca/CifradorCpfAesGcm.cs
│   └── Persistencia/{Configuracoes/UsuarioConfiguration.cs, Migrations/*}
└── Identity.Api/Endpoints/DoadoresEndpoints.cs
tests/unit/Identity.UnitTests/Usuarios/{CpfTests.cs, SenhaTests.cs, ConsentimentoTests.cs, UsuarioCadastroTests.cs}
tests/integration/Identity.IntegrationTests/CadastroDoadorTests.cs
tests/e2e/ConexaoSolidaria.Bdd/Features/003-cadastro-doador.feature
```

## Design

1. Validador (formato) → 400 com `errors`.
2. Caso de uso cria os VOs; falha → 400.
3. Calcula `cpf_hash` e consulta e-mail e hash; existente → 409.
4. `Usuario.CadastrarDoador(...)` com senha já em hash e CPF cifrado.
5. `SaveChanges`; violação `23505` → 409 (corrida).
6. Resposta 201 com `cpfMascarado`.

## Observabilidade

| Sinal | Definição |
|---|---|
| Métrica | `doadores_cadastrados_total` |
| Log | `Information` "Doador {UsuarioId} cadastrado"; nunca nome, e-mail ou CPF |
| Trace | Span `CadastrarDoador` sem atributos pessoais |

## Riscos

| Risco | Mitigação |
|---|---|
| Perda da chave de cifra | Chave em Secret com backup no cofre do grupo; versão da chave no dado |
| Vazamento por mensagem de erro | Mensagens fixas, sem eco dos valores; teste sobre o corpo do 400 |
