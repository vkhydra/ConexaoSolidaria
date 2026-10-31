# Modelo de dados — Feature 003 (Cadastro de doador)

[← Spec](spec.md)

## Agregado `Usuario` — criação de doador

| Método | Regra |
|---|---|
| `CadastrarDoador(nome, email, cpf, senhaHash, consentimento, agora)` | Role sempre `Doador`; exige consentimento; gera `UsuarioId` v7 |

Campos acrescentados por esta feature (os de login estão em [001](../001-autenticacao/data-model.md)):

| Membro | Tipo | Regra |
|---|---|---|
| `NomeCompleto` | `string` | 3–150 caracteres, ao menos duas palavras |
| `Cpf` | `Cpf` (VO) | Só para doadores; persistido cifrado + hash |
| `Consentimento` | `Consentimento` (VO) | Versão da política e data do aceite |
| `CriadoEm` | `DateTimeOffset` | `TimeProvider` |

## Value objects

| VO | Regra | Igualdade | Erro |
|---|---|---|---|
| `Cpf` | 11 dígitos; dígitos verificadores válidos; sem sequência repetida; `Mascarado()` → `***.982.247-**` | Por dígitos | `cpf.invalido` |
| `Email` | Formato válido; até 254; minúsculas | Por valor normalizado | `email.invalido` |
| `Senha` | 8–64; letra, número e especial (só validação) | — | `senha.fraca` |
| `Consentimento` | Versão não vazia e data | Por valor | `consentimento.obrigatorio` |

## Tabela `usuarios` (`identity_db`)

| Coluna | Tipo | Restrições |
|---|---|---|
| `id` | `uuid` | PK |
| `nome_completo` | `varchar(150)` | NOT NULL |
| `email` | `varchar(254)` | NOT NULL, UNIQUE |
| `cpf_cifrado` | `bytea` | NULL (gestor e anonimizado) |
| `cpf_hash` | `bytea` | NULL, UNIQUE parcial (`WHERE cpf_hash IS NOT NULL`) |
| `senha_hash` | `varchar(100)` | NULL após anonimizar |
| `role` | `varchar(20)` | NOT NULL, CHECK (`GestorONG`, `Doador`) |
| `consentimento_versao` | `varchar(20)` | NULL para gestor |
| `consentimento_em` | `timestamptz` | NULL para gestor |
| `criado_em` | `timestamptz` | NOT NULL |
| `anonimizado_em` | `timestamptz` | NULL |

## Formato do CPF cifrado

```text
| 1 byte versão da chave | 12 bytes nonce | 16 bytes tag | N bytes cifra |
```

Chaves: `Cpf__ChaveCifra` e `Cpf__ChaveHash` (256 bits, Base64, em Secret).
