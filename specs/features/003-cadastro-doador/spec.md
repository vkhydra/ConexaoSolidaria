# Feature 003 — Cadastro de doador

[← Visão geral do produto](../../product/overview.md)

| Campo | Valor |
|---|---|
| Branch | `feat/003-cadastro-doador` |
| Status | Especificada |
| Origem | Enunciado, requisito 3 (RF03) |
| História | H01 |
| Prioridade | P1 |
| Serviço | `Identity.Api` |
| Acesso | Público |
| Depende de | [001 — Autenticação](../001-autenticacao/spec.md) (agregado `Usuario` e `IdentityDbContext`) |

## Objetivo

Permitir que qualquer pessoa crie uma conta de doador com e-mail único, CPF válido e senha forte, guardando os dados pessoais com segurança e com consentimento registrado.

## História de usuário

> **H01** — Como visitante, quero me cadastrar como doador, para poder doar.

## Cenários de aceitação

```gherkin
# language: pt
Funcionalidade: Cadastro de doador

  Cenário: Cadastro válido
    Quando um visitante se cadastra com nome "Maria Silva", e-mail "Maria@Exemplo.com", CPF "529.982.247-25" e senha "Senha@123"
    Então recebe status 201
    E o e-mail é salvo como "maria@exemplo.com"
    E a senha não é salva em texto puro
    E a resposta mostra o CPF como "***.982.247-**"

  Cenário: E-mail duplicado
    Dado um doador cadastrado com e-mail "maria@exemplo.com"
    Quando outro visitante se cadastra com o e-mail "MARIA@exemplo.com"
    Então recebe status 409
    E o código do erro é "doador.email_em_uso"

  Cenário: CPF duplicado com outra máscara
    Dado um doador cadastrado com CPF "529.982.247-25"
    Quando outro visitante se cadastra com o CPF "52998224725"
    Então recebe status 409
    E o código do erro é "doador.cpf_em_uso"

  Esquema do Cenário: CPF inválido
    Quando um visitante se cadastra com CPF "<cpf>"
    Então recebe status 400

    Exemplos:
      | cpf            |
      | 529.982.247-26 |
      | 111.111.111-11 |
      | 123            |

  Esquema do Cenário: Senha fraca
    Quando um visitante se cadastra com a senha "<senha>"
    Então recebe status 400

    Exemplos:
      | senha     |
      | curta@1   |
      | semnumero@ |
      | Sem1Especial |

  Cenário: Sem aceite da política de privacidade
    Quando um visitante se cadastra sem aceitar a política de privacidade
    Então recebe status 400
```

## Requisitos funcionais

| ID | Requisito |
|---|---|
| FR-001 | O cadastro DEVE ser público e criar sempre um usuário com role `Doador`. |
| FR-002 | Nome completo DEVE ter 3 a 150 caracteres e ao menos duas palavras. |
| FR-003 | E-mail DEVE ser válido, ter até 254 caracteres, ser normalizado em minúsculas e ser único (409 `doador.email_em_uso`). |
| FR-004 | CPF DEVE aceitar entrada com ou sem máscara, ter 11 dígitos e dígitos verificadores válidos, e rejeitar sequências repetidas. |
| FR-005 | CPF DEVE ser único, comparado pelo hash (409 `doador.cpf_em_uso`). |
| FR-006 | Senha DEVE ter 8 a 64 caracteres, com letra, número e caractere especial, e ser guardada só como hash BCrypt. |
| FR-007 | O CPF DEVE ser guardado cifrado (AES-256-GCM) e com hash HMAC-SHA256; NUNCA em texto. |
| FR-008 | O aceite da política de privacidade DEVE ser obrigatório e registrado com versão e data. |
| FR-009 | A resposta (201) NÃO DEVE conter a senha e DEVE mostrar o CPF mascarado. |

## Casos de borda

- E-mail com espaços nas pontas → 400.
- CPF com letras ou com 10/12 dígitos → 400.
- Dois cadastros simultâneos com o mesmo e-mail → um recebe 201 e o outro 409 (constraint única no banco).
- E-mail ou CPF de usuário anonimizado → pode ser reutilizado ([008](../008-direitos-titular/spec.md)).

## Entidades-chave

- **Usuario** (doador), **Cpf**, **Email**, **Senha** e **Consentimento** ([data-model](data-model.md)).

## Critérios de sucesso

| ID | Critério |
|---|---|
| SC-001 | Nenhum CPF, senha ou e-mail aparece em log, trace ou métrica (teste sobre a saída de log). |
| SC-002 | 100% das regras do `Cpf` cobertas por teste de unidade (válido, inválido, repetido, tamanho, máscara). |
| SC-003 | Cadastro p95 < 500 ms (BCrypt incluso). |

## Fora do escopo

Confirmação de e-mail, cadastro de pessoa jurídica, edição dos dados cadastrais (só acesso e anonimização em [008](../008-direitos-titular/spec.md)).

## Esclarecimentos

### Sessão 2026-10-30
- **P:** Qual a versão inicial da política de privacidade? → **R:** `2026-10`, definida em configuração (`Lgpd__VersaoPolitica`) e exibida no formulário.
