# Contrato HTTP — Feature 008 (Direitos do titular)

[← Spec](../spec.md)

## `GET /doadores/me` — `Doador`

```json
// 200 OK
{
  "id": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
  "nomeCompleto": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpfMascarado": "***.982.247-**",
  "criadoEm": "2026-11-10T14:03:22-03:00",
  "consentimento": { "versao": "2026-10", "aceitoEm": "2026-11-10T14:03:22-03:00" }
}
```

| Status | Quando |
|---|---|
| 401 | Sem token, token inválido ou usuário anonimizado |
| 403 | Token sem role `Doador` |

Cabeçalho `Cache-Control: no-store` (dado pessoal).

## `DELETE /doadores/me` — `Doador`

204 No Content. Erros: 401, 403.

Efeito: anonimização descrita no [modelo de dados](../data-model.md). A interface pede confirmação antes de chamar.
