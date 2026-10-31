# Contrato HTTP — Feature 001 (Autenticação)

[← Spec](../spec.md)

Base: `https://api.conexao.local/api/v1`. Convenções gerais (erros, formato, correlação) em [API e eventos](../../../product/api-e-eventos.md).

## `POST /auth/login` — público

```json
// Requisição
{ "email": "maria@exemplo.com", "senha": "Senha@123" }

// 200 OK
{ "accessToken": "eyJhbGciOiJSUzI1NiIs...", "tokenType": "Bearer", "expiresIn": 3600, "role": "Doador" }
```

| Status | Código | Quando |
|---|---|---|
| 400 | `validacao` | E-mail ou senha ausentes ou mal formados |
| 401 | `auth.credenciais_invalidas` | E-mail inexistente, senha errada ou usuário anonimizado |
| 429 | `auth.bloqueado` | 5 falhas em 15 min (Identity) ou limite do Kong (10/min por IP) |

Exemplo de 401:
```json
{
  "type": "https://conexao.local/erros/auth.credenciais_invalidas",
  "title": "E-mail ou senha inválidos",
  "status": 401,
  "code": "auth.credenciais_invalidas",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

## Uso do token

- Cabeçalho `Authorization: Bearer <accessToken>` em toda rota protegida.
- Sem token ou token inválido → 401 com `WWW-Authenticate: Bearer`.
- Token válido sem a role exigida → 403.

## Policies

| Policy | Claim exigida | Rotas |
|---|---|---|
| `GestorONG` | `role = GestorONG` | `/campanhas/**` (exceto `/campanhas/busca`), `/admin/**` |
| `Doador` | `role = Doador` | `/doacoes`, `/doadores/me` |
| Pública | — | `POST /doadores`, `POST /auth/login`, `/transparencia/**`, `/campanhas/busca` |
