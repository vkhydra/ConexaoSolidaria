# Pesquisa — Feature 001 (Autenticação)

[← Spec](spec.md)

Decisões técnicas tomadas antes do plano. Cada item traz a decisão, a justificativa e as alternativas.

## R1 — Algoritmo de assinatura do JWT

- **Decisão:** RS256 com chave RSA 2048. A Identity assina com a chave privada (Secret); os demais validam com a pública (ConfigMap).
- **Justificativa:** quem valida não consegue emitir tokens.
- **Alternativas:** HS256 (chave compartilhada; qualquer serviço poderia emitir); Keycloak/OIDC externo (fora do escopo do MVP).
- **Registro:** [ADR 0008](../../../docs/adr/0008-jwt-rs256-emitido-pela-identity.md).

## R2 — Onde guardar o contador de tentativas

- **Decisão:** tabela `tentativas_login` no `identity_db`, com chave `email_hash` (HMAC do e-mail normalizado).
- **Justificativa:** sem dependência nova na Identity; sobrevive a reinícios; não guarda o e-mail em claro.
- **Alternativas:** Redis (mais uma dependência na Identity); memória do processo (não funciona com 2 réplicas).

## R3 — Custo do hash de senha

- **Decisão:** BCrypt (`BCrypt.Net-Next`) com work factor 12.
- **Justificativa:** cerca de 250 ms por verificação em hardware comum; seguro contra força bruta offline sem pesar no login.
- **Alternativas:** PBKDF2 do `PasswordHasher` do ASP.NET Identity (bom, mas o projeto não usa ASP.NET Identity); Argon2 (sem biblioteca .NET oficial).

## R4 — Evitar descoberta de e-mails cadastrados

- **Decisão:** quando o e-mail não existe, verificar a senha contra um hash BCrypt fixo antes de responder 401.
- **Justificativa:** o tempo de resposta fica igual ao de senha errada; a mensagem também é a mesma (FR-004).

## R5 — Validação do token nos outros serviços

- **Decisão:** `AddJwtBearer` configurado no `ConexaoSolidaria.ServiceDefaults`, lendo `Jwt__Issuer`, `Jwt__Audience` e `Jwt__ChavePublicaPem`; `ClockSkew` de 30 s; `MapInboundClaims = false`.
- **Justificativa:** uma configuração para todos os serviços; claims com os nomes do JWT (`sub`, `role`).
- **Fonte:** MCP Microsoft Learn, "Configure JWT bearer authentication in ASP.NET Core".

## R6 — Gestor inicial

- **Decisão:** `IHostedService` de seed na Identity que roda após as migrations e cria o gestor só se não houver nenhum `GestorONG`.
- **Justificativa:** idempotente; não depende de script manual; credenciais só em Secret.
