# Tarefas — Feature 003 (Cadastro de doador)

[← Plano](plan.md)

Formato e regras: iguais às da [feature 001](../001-autenticacao/tasks.md).

## Fase 1 — Domínio (TDD)

- [X] T001 [H01] VO `Cpf` com dígitos verificadores, sequências repetidas e `Mascarado()`, com testes — `Identity.Domain/Usuarios/Cpf.cs`
  Commit: `✨ feat(identity): adiciona value object Cpf com validação de dígitos`
- [X] T002 [P] [H01] VO `Senha` com a política de segurança, com testes — `Identity.Domain/Usuarios/Senha.cs`
  Commit: `✨ feat(identity): adiciona value object Senha com política de segurança`
- [X] T003 [P] [H01] VO `Consentimento`, com testes — `Identity.Domain/Usuarios/Consentimento.cs`
  Commit: `✨ feat(identity): adiciona value object Consentimento`
- [X] T004 [H01] `Usuario.CadastrarDoador` com testes — `Identity.Domain/Usuarios/Usuario.cs`
  Commit: `✨ feat(identity): adiciona cadastro de doador no agregado Usuario`

## Fase 2 — Aplicação

- [X] T005 [H01] `ICifradorCpf` e caso de uso `CadastrarDoador` com validador e testes (sucesso, 400, 409 e-mail, 409 CPF) — `Identity.Application/Doadores/*`
  Commit: `✨ feat(identity): adiciona caso de uso de cadastro de doador`

## Fase 3 — Infraestrutura

- [X] T006 [H01] `CifradorCpfAesGcm` (cifra, decifra, hash, versão da chave), com testes — `Identity.Infrastructure/Seguranca/CifradorCpfAesGcm.cs`
  Commit: `🔒️ feat(identity): adiciona cifragem e hash do cpf`
- [X] T007 [H01] Configuração EF das colunas de cadastro e migration — `Identity.Infrastructure/Persistencia/*`
  Commit: `🗃️ feat(identity): adiciona colunas de cadastro de doador`

## Fase 4 — API

- [X] T008 [H01] Endpoint `POST /doadores` (`AllowAnonymous`), tratamento de `23505` como 409 — `Identity.Api/Endpoints/DoadoresEndpoints.cs`
  Commit: `✨ feat(identity): adiciona endpoint de cadastro de doador`
- [X] T009 [P] [H01] Métrica `doadores_cadastrados_total` — `Identity.Application/Doadores/CadastrarDoador.cs`
  Commit: `📈 feat(identity): adiciona métrica de doadores cadastrados`

## Fase 5 — Testes de integração e aceitação

- [X] T010 [H01] Testes de integração: 201, 400 (cada regra), 409 e-mail, 409 CPF, CPF cifrado no banco, log sem dados pessoais — `tests/integration/Identity.IntegrationTests/CadastroDoadorTests.cs`
  Commit: `✅ test(identity): adiciona testes de integração do cadastro de doador`
- [X] T011 [H01] Cenários Gherkin automatizados — `tests/e2e/ConexaoSolidaria.Bdd/Features/003-cadastro-doador.feature`
  Commit: `✅ test(bdd): adiciona cenários de cadastro de doador`

## Fase 6 — Interface

- [X] T012 [H01] Página `/cadastro` com `EditForm`, validação, link para a política e testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Cadastro.razor(.cs)`
  Commit: `✨ feat(web): adiciona cadastro de doador`

## Fase 7 — Fechamento

- [X] T013 `/security-review` e `/code-review`; cobertura ≥ 90%; `git rebase --exec` antes do PR

## Dependências

T001, T002, T003 → T004 → T005 → T008; T006 → T008; T007 → T008 → T010 → T011; T008 → T012.
