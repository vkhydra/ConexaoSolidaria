# Tarefas — Feature 001 (Autenticação)

[← Plano](plan.md)

Formato: `- [ ] Tnnn [P] [história] descrição — arquivo(s)`, seguido do commit. `[P]` = pode ser feita em paralelo com a anterior (arquivos diferentes, sem dependência). Cada tarefa termina com build, testes e cobertura passando ([constituição](../../constitution.md), princípios III, IV e VI).

## Fase 1 — Testes e domínio (TDD)

- [X] T001 [H02] Value object `Email` com normalização e validação, com testes — `Identity.Domain/Usuarios/Email.cs`, `tests/unit/Identity.UnitTests/Usuarios/EmailTests.cs`
  Commit: `✨ feat(identity): adiciona value object Email`
- [X] T002 [H02] `Role`, `UsuarioId` e agregado `Usuario` com `CriarGestor` e `VerificarSenha`, com testes — `Identity.Domain/Usuarios/*`
  Commit: `✨ feat(identity): adiciona agregado Usuario com verificação de senha`
- [X] T003 [P] [H02] Entidade `TentativaLogin` com janela de 15 min e bloqueio na 5ª falha, com testes — `Identity.Domain/Autenticacao/TentativaLogin.cs`
  Commit: `✨ feat(identity): adiciona controle de tentativas de login`

## Fase 2 — Aplicação

- [X] T004 [H02] Interfaces `IUsuarioRepositorio`, `IHasherEmail`, `IEmissorToken`, `ITentativaLoginRepositorio` — `Identity.Application/Abstracoes/*` (`IHasherSenha` ficou no domínio, porque `Usuario.VerificarSenha` a usa)
  Commit: `🏷️ feat(identity): adiciona abstrações de autenticação`
- [X] T005 [H02] Caso de uso `Autenticar` com validador e testes (sucesso, senha errada, e-mail inexistente com tempo constante, bloqueado, anonimizado) — `Identity.Application/Autenticacao/*`
  Commit: `✨ feat(identity): adiciona caso de uso de autenticação`

## Fase 3 — Infraestrutura

- [X] T006 [H02] `IdentityDbContext`, configurações de `usuarios` e `tentativas_login` e migration inicial — `Identity.Infrastructure/Persistencia/*`
  Commit: `🗃️ feat(identity): adiciona IdentityDbContext e migration inicial`
- [X] T007 [P] [H02] `HasherSenhaBcrypt` (work factor 12) com testes — `Identity.Infrastructure/Seguranca/HasherSenhaBcrypt.cs`
  Commit: `🛂 feat(identity): adiciona hash de senha com bcrypt`
- [ ] T008 [P] [H02] `EmissorJwtRs256` com testes do payload (sem dados pessoais) — `Identity.Infrastructure/Seguranca/EmissorJwtRs256.cs`
  Commit: `🛂 feat(identity): emite jwt rs256 no login`
- [ ] T009 [H02] Repositórios EF Core com testes de integração (Testcontainers) — `Identity.Infrastructure/Persistencia/Repositorios/*`
  Commit: `🗃️ feat(identity): adiciona repositórios de usuário e tentativas`

## Fase 4 — Validação do token em todos os serviços

- [ ] T010 [H02] `AddAutenticacaoJwt()` com policies `GestorONG`, `Doador` e `FallbackPolicy`, com testes — `ConexaoSolidaria.ServiceDefaults/Autenticacao/*`
  Commit: `🛂 feat(defaults): adiciona validação de jwt e policies por role`

## Fase 5 — API

- [ ] T011 [H02] Endpoint `POST /auth/login` com `TypedResults` e `ProblemDetails` — `Identity.Api/Endpoints/AutenticacaoEndpoints.cs`
  Commit: `✨ feat(identity): adiciona endpoint de login`
- [ ] T012 [H02] Seed do gestor por `Seed__GestorEmail`/`Seed__GestorSenha`, com teste de idempotência — `Identity.Infrastructure/Seed/SeedGestorHostedService.cs`
  Commit: `🌱 chore(identity): adiciona seed do gestor por variáveis de ambiente`
- [ ] T013 [H02] Métrica `logins_total{resultado}` e logs sem e-mail — `Identity.Application/Autenticacao/Autenticar.cs`
  Commit: `📈 feat(identity): adiciona métrica de logins`

## Fase 6 — Testes de integração e aceitação

- [ ] T014 [H02] Testes de integração: 200, 401 (dois casos), 429, 401 sem token, 403 role errada — `tests/integration/Identity.IntegrationTests/AutenticacaoTests.cs`
  Commit: `✅ test(identity): adiciona testes de integração do login`
- [ ] T015 [H02] Cenários Gherkin da spec automatizados — `tests/e2e/ConexaoSolidaria.Bdd/Features/001-autenticacao.feature` e steps
  Commit: `✅ test(bdd): adiciona cenários de autenticação`

## Fase 7 — Interface

- [ ] T016 [H02] Página `/entrar`, cookie de autenticação no servidor e `DelegatingHandler` que anexa o JWT, com testes bUnit — `ConexaoSolidaria.Web/Components/Pages/Entrar.razor(.cs)`
  Commit: `🛂 feat(web): adiciona login e sessão autenticada`

## Fase 8 — Fechamento

- [ ] T017 `/security-review` e `/code-review`; corrigir apontamentos em commits próprios
- [ ] T018 Conferir cobertura ≥ 90% (`verificar-cobertura`) e `git rebase --exec` antes do PR

## Dependências

T001 → T002 → T005; T003 → T005; T004 → T005 → T011; T006 → T009 → T011; T007, T008 → T011; T010 → T014; T011, T012 → T014 → T015; T011 → T016.
