# Feature 008 — Direitos do titular (LGPD)

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/008-direitos-titular` |
| Status | Especificada |
| Origem | Fase 5 (LGPD) — RF08 |
| História | H10 |
| Prioridade | P2 |
| Serviço | `Identity.Api` |
| Acesso | `Doador` |
| Depende de | [001](../001-autenticacao/spec.md), [003](../003-cadastro-doador/spec.md) |

## Objetivo

Permitir que o doador veja os próprios dados e peça a eliminação deles, cumprindo os direitos de acesso e eliminação da LGPD sem apagar a prestação de contas das doações.

## História de usuário

> **H10** — Como doador, quero ver e apagar meus dados, para exercer meus direitos da LGPD.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Direitos do titular

  Cenário: Ver meus dados
    Dado um doador autenticado cadastrado com o CPF "529.982.247-25"
    Quando ele consulta os seus dados
    Então recebe status 200
    E vê o CPF como "***.982.247-**"
    E vê a versão e a data do aceite da política de privacidade

  Cenário: Anonimizar cadastro
    Dado um doador autenticado que já doou 100.00
    Quando ele solicita a exclusão dos seus dados
    Então recebe status 204
    E não consegue mais fazer login
    E o total da campanha continua incluindo os 100.00

  Cenário: Token após anonimização
    Dado um doador que acabou de anonimizar o cadastro
    Quando ele consulta os seus dados com o mesmo token
    Então recebe status 401

  Cenário: Reutilizar e-mail e CPF após anonimizar
    Dado um doador anonimizado que usava o e-mail "maria@exemplo.com"
    Quando um visitante se cadastra com o e-mail "maria@exemplo.com"
    Então recebe status 201
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | `GET /doadores/me` DEVE devolver nome, e-mail, CPF mascarado, data de cadastro e versão e data do aceite. |
| FR-002 | `DELETE /doadores/me` DEVE anonimizar: nome → `Doador anonimizado`; e-mail → `anonimo+<id>@invalid`; CPF cifrado, hash e senha removidos; `anonimizadoEm` preenchido. |
| FR-003 | Após anonimizar, o login DEVE ser recusado e `/doadores/me` DEVE responder 401 com o token antigo. |
| FR-004 | As doações DEVEM continuar no total e no livro de doações, ligadas só ao ID (que não identifica mais ninguém). |
| FR-005 | A operação DEVE ser registrada em log de auditoria sem dados pessoais. |
| FR-006 | A anonimização DEVE ser idempotente (repetir não gera erro no domínio). |

## Casos de borda

- Gestor chamando `/doadores/me` → 403 (policy `Doador`).
- Usuário removido entre a emissão do token e a chamada → 401.
- Falha no meio da anonimização → transação desfeita; nada parcialmente anonimizado.

## Entidades-chave

- **Usuario** com o método `Anonimizar` ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | Após anonimizar, nenhuma coluna pessoal do usuário contém o dado original (teste no banco). |
| SC-002 | O total das campanhas não muda com a anonimização. |
| SC-003 | O log de auditoria contém só `UsuarioId`, ação e data. |

## Fora do escopo

Portabilidade (exportar dados em arquivo), correção de dados cadastrais e revogação parcial do consentimento.

## Esclarecimentos

### Sessão 2026-10-30
- **P:** Por que anonimizar em vez de apagar a linha? → **R:** a prestação de contas exige manter as doações (legítimo interesse); sem dados pessoais, o registro deixa de ser dado pessoal ([segurança e LGPD](../../product/seguranca-e-lgpd.md)).
- **P:** Como o token antigo é recusado na Identity? → **R:** a Identity confere `anonimizado_em` do `sub` em `/doadores/me`; nos outros serviços o token expira em até 60 min (risco aceito).
