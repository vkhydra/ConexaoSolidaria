# ADR 0008 — JWT RS256 emitido só pela Identity

- **Status:** Aceito
- **Data:** 2026-10-30
- **Decisores:** equipe do projeto

## Contexto

com chave simétrica (HS256), qualquer serviço que valida também poderia emitir tokens.

## Decisão

Identity assina com chave privada RSA 2048 (Secret); os demais validam com a chave pública (ConfigMap). Validade de 60 min; validação de `iss`, `aud`, `exp` e assinatura com tolerância de 30 s.

## Alternativas consideradas

HS256 (mais simples, menos seguro); provedor OIDC externo (Keycloak): fora do escopo do MVP.

## Consequências

rotação de chave exige atualizar Secret e ConfigMap; documentado no runbook.

## Referências

- [Feature 001 — Autenticação](../../specs/features/001-autenticacao/plan.md)
- [Segurança e LGPD](../../specs/product/seguranca-e-lgpd.md)
- [Runbook de rotação da chave do JWT](../runbooks/rotacao-chave-jwt.md)
