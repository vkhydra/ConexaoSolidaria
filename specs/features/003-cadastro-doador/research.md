# Pesquisa — Feature 003 (Cadastro de doador)

[← Spec](spec.md)

## R1 — Cifragem do CPF

- **Decisão:** `System.Security.Cryptography.AesGcm` com chave de 256 bits. Formato gravado: `versãoChave (1 byte) || nonce (12) || tag (16) || cifra`.
- **Justificativa:** cifra autenticada da própria BCL; a versão da chave permite rotação sem reescrever tudo de uma vez.
- **Alternativas:** criptografia do banco (`pgcrypto`: chave chegaria ao banco); ASP.NET Data Protection (formato próprio, chaves em arquivo).

## R2 — Unicidade sem decifrar

- **Decisão:** `cpf_hash = HMAC-SHA256(chaveHash, cpfSomenteDigitos)` com índice único parcial (`WHERE cpf_hash IS NOT NULL`).
- **Justificativa:** busca por igualdade sem expor o CPF; HMAC com chave impede ataque de dicionário (só existem 10⁹ CPFs).
- **Alternativas:** SHA-256 puro (quebrável por força bruta em minutos).

## R3 — Validação do CPF

- **Decisão:** VO `Cpf` com algoritmo dos dois dígitos verificadores (módulo 11) e rejeição de sequências repetidas.
- **Justificativa:** regra pura, testável com TDD; sem consulta externa (Receita Federal fora do escopo).

## R4 — Política de senha

- **Decisão:** VO `Senha` só para validação (nunca guardado): 8–64 caracteres, ao menos uma letra, um número e um caractere não alfanumérico.
- **Justificativa:** atende a regra do RF03 e o limite de 72 bytes do BCrypt (64 caracteres ASCII cabem).

## R5 — Corrida entre cadastros iguais

- **Decisão:** além da checagem prévia, tratar violação de constraint única (`23505`) do PostgreSQL como 409.
- **Justificativa:** duas requisições simultâneas passam pela checagem; o banco é a garantia final.

## R6 — Base legal do CPF

- **Decisão:** consentimento (art. 7º, I) registrado no aceite; revisar se surgir obrigação legal de emitir recibo fiscal.
- **Fonte:** `wiki:disciplinas/lgpd`; detalhes em [segurança e LGPD](../../product/seguranca-e-lgpd.md).
