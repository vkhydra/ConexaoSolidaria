# Contrato HTTP — Feature 003 (Cadastro de doador)

[← Spec](../spec.md)

Base: `https://api.conexao.local/api/v1`.

## `POST /doadores` — público

```json
// Requisição
{
  "nomeCompleto": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpf": "529.982.247-25",
  "senha": "Senha@123",
  "aceitePoliticaPrivacidade": true
}

// 201 Created — Location: /api/v1/doadores/me
{
  "id": "0192f3a1-7c2e-7b4a-9f1e-2b3c4d5e6f70",
  "nomeCompleto": "Maria Silva",
  "email": "maria@exemplo.com",
  "cpfMascarado": "***.982.247-**",
  "criadoEm": "2026-11-10T14:03:22-03:00"
}
```

| Status | Código | Quando |
|---|---|---|
| 400 | `validacao` | Campo ausente, formato inválido, CPF inválido, senha fraca ou sem aceite |
| 409 | `doador.email_em_uso` | E-mail já cadastrado |
| 409 | `doador.cpf_em_uso` | CPF já cadastrado |
| 429 | — | Limite do Kong (30/min por IP) |

Exemplo de 400:
```json
{
  "type": "https://conexao.local/erros/validacao",
  "title": "Dados inválidos",
  "status": 400,
  "code": "validacao",
  "errors": { "cpf": ["CPF inválido."], "senha": ["A senha precisa ter letra, número e caractere especial."] },
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Mensagens de validação nunca repetem o valor enviado (evita dado pessoal em logs de erro).
