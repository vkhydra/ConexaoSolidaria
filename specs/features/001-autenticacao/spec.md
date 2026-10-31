# Feature 001 — Autenticação e autorização (RBAC)

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/001-autenticacao` |
| Status | Especificada |
| Origem | Enunciado, requisito 1 (RF01) |
| História | H02 |
| Prioridade | P1 (bloqueia todas as funcionalidades protegidas) |
| Serviço | `Identity.Api` (emite o token); todos os serviços validam |
| Depende de | Etapa 3 do [plano de execução](../../../docs/plano-de-execucao.md) (solução e `ServiceDefaults`) |

## Objetivo

Permitir que doadores e gestores entrem com e-mail e senha e recebam um token que dá acesso só ao que o seu perfil pode fazer.

## História de usuário

> **H02** — Como doador ou gestor, quero entrar com e-mail e senha, para acessar minhas funções.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Login e controle de acesso

  Cenário: Login válido de doador
    Dado um doador cadastrado com e-mail "maria@exemplo.com" e senha "Senha@123"
    Quando ele faz login com essas credenciais
    Então recebe status 200
    E o token contém a role "Doador"
    E o token não contém nome, e-mail nem CPF

  Cenário: E-mail com letras maiúsculas
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando ele faz login com o e-mail "Maria@Exemplo.com" e a senha correta
    Então recebe status 200

  Cenário: Senha incorreta
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando ele faz login com a senha "errada"
    Então recebe status 401
    E a mensagem é "E-mail ou senha inválidos"

  Cenário: E-mail inexistente
    Quando alguém faz login com o e-mail "ninguem@exemplo.com"
    Então recebe status 401
    E a mensagem é "E-mail ou senha inválidos"

  Cenário: Bloqueio por tentativas
    Dado 5 tentativas de login com senha incorreta para "maria@exemplo.com" em 15 minutos
    Quando ele tenta entrar novamente com a senha correta
    Então recebe status 429

  Cenário: Rota protegida sem token
    Quando alguém lista as campanhas da gestão sem token
    Então recebe status 401

  Cenário: Doador tenta acessar gestão de campanhas
    Dado um doador autenticado
    Quando ele lista as campanhas da gestão
    Então recebe status 403

  Cenário: Gestor inicial criado na implantação
    Dado as variáveis de seed do gestor configuradas
    Quando a Identity.Api inicia sem nenhum gestor no banco
    Então o gestor é criado com a role "GestorONG"
    E uma segunda inicialização não cria outro gestor
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | O sistema DEVE autenticar por e-mail e senha, comparando o e-mail em minúsculas. |
| FR-002 | O sistema DEVE emitir um token JWT assinado com RS256, válido por 60 minutos. |
| FR-003 | O token DEVE conter só `sub`, `role`, `jti`, `iat`, `exp`, `iss` e `aud`; NÃO DEVE conter nome, e-mail ou CPF. |
| FR-004 | Falha de login DEVE responder 401 com a mesma mensagem genérica para e-mail inexistente e senha errada. |
| FR-005 | Após 5 falhas seguidas no mesmo e-mail em 15 minutos, o login desse e-mail DEVE ficar bloqueado por 15 minutos (429). |
| FR-006 | Usuário anonimizado NÃO DEVE conseguir entrar. |
| FR-007 | Todos os serviços DEVEM validar assinatura, `iss`, `aud`, `exp` e `nbf`, com tolerância de 30 s. |
| FR-008 | Os serviços DEVEM oferecer as policies `GestorONG` e `Doador`; endpoint sem policy DEVE exigir autenticação (`FallbackPolicy`). |
| FR-009 | O primeiro `GestorONG` DEVE ser criado na inicialização a partir de `Seed__GestorEmail` e `Seed__GestorSenha`, só se não existir. |

## Casos de borda

- Token expirado, com assinatura inválida ou de outro emissor → 401.
- Token emitido antes da anonimização continua válido até expirar (máximo 60 min): risco aceito em [segurança e LGPD](../../product/seguranca-e-lgpd.md).
- O bloqueio termina sozinho após 15 minutos; um login certo antes do bloqueio zera a contagem.
- Variáveis de seed ausentes → a API inicia sem criar gestor e registra `Warning`.

## Entidades-chave

- **Usuario:** identidade, e-mail, hash da senha, role, data de anonimização ([data-model](data-model.md)).
- **Tentativa de login:** contagem de falhas por e-mail e fim do bloqueio.

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | 100% dos endpoints não públicos respondem 401 sem token e 403 com role errada (testes de integração). |
| SC-002 | Nenhum token emitido contém dado pessoal (teste automatizado sobre o payload). |
| SC-003 | A 6ª tentativa de login após 5 falhas responde 429. |
| SC-004 | O Kong limita o login a 10 requisições por minuto por IP (RNF20). |

## Fora do escopo

Refresh token, recuperação de senha, login social, autenticação em dois fatores e cadastro de gestor pela interface.

## Esclarecimentos

### Sessão 2026-10-30
- **P:** Qual o nome das variáveis do gestor inicial? → **R:** `Seed__GestorEmail` e `Seed__GestorSenha` (padrão de configuração do .NET, igual ao restante da [arquitetura](../../product/arquitetura.md)).
- **P:** O bloqueio é por e-mail ou por IP? → **R:** por e-mail na Identity (FR-005) e por IP no Kong (SC-004).
