# Pesquisa — Feature 008 (Direitos do titular)

[← Spec](spec.md)

## R1 — Anonimização × exclusão

- **Decisão:** anonimizar o registro do usuário e manter o ID.
- **Justificativa:** o livro de doações e o total da campanha guardam o `doadorId`; apagar a linha quebraria a prestação de contas. Sem nome, e-mail e CPF, o ID deixa de identificar a pessoa (LGPD, art. 12).
- **Alternativas:** exclusão física em cascata (perde a prestação de contas); pseudonimização reversível (continua sendo dado pessoal).
- **Fonte:** `wiki:disciplinas/lgpd`, `wiki:boas-praticas/lgpd`.

## R2 — Recusar o token de usuário anonimizado

- **Decisão:** na Identity, `/doadores/me` carrega o usuário pelo `sub` e responde 401 se `anonimizado_em` estiver preenchido. O login também recusa.
- **Justificativa:** a Campanhas.Api não consulta a Identity (sem chamada síncrona entre contextos); o token restante expira em até 60 min.
- **Alternativas:** lista de tokens revogados no Redis compartilhado (acoplamento entre contextos; fora do MVP).

## R3 — E-mail após anonimizar

- **Decisão:** `anonimo+<id>@invalid` (domínio reservado `.invalid`, RFC 2606).
- **Justificativa:** mantém a constraint `NOT NULL UNIQUE` e libera o e-mail original para novo cadastro.

## R4 — Auditoria

- **Decisão:** log estruturado `Information` com `EventId` `TitularAnonimizado`, `UsuarioId` e data; sem tabela extra.
- **Justificativa:** responsabilização (accountability) sem guardar dado pessoal.
