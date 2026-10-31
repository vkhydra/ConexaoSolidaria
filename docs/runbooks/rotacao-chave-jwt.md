# Runbook — Rotação da chave do JWT

[← Runbooks](README.md)

| Item | Valor |
|---|---|
| Tipo | Operação planejada |
| Quando | Suspeita de vazamento da chave privada ou rotina periódica |
| Decisão relacionada | [ADR 0008](../adr/0008-jwt-rs256-emitido-pela-identity.md) |

## Impacto

Tokens assinados com a chave antiga deixam de ser aceitos depois da troca: usuários logados precisam entrar de novo (sessão de até 60 min).

## Procedimento

1. Gerar o novo par de chaves (em `pwsh`, em qualquer sistema):
   ```powershell
   $rsa = [System.Security.Cryptography.RSA]::Create(2048)
   $rsa.ExportPkcs8PrivateKeyPem() | Set-Content jwt-privada-nova.pem
   $rsa.ExportSubjectPublicKeyInfoPem() | Set-Content jwt-publica-nova.pem
   ```
2. Atualizar `Jwt__ChavePrivadaPem` e `Jwt__ChavePublicaPem` no `deploy/.env` (arquivo ignorado pelo git).
3. Atualizar primeiro a **chave pública** (ConfigMap das APIs) e reiniciar quem valida:
   ```bash
   pwsh ./scripts/k8s-secrets.ps1 -EnvFile deploy/.env
   kubectl apply -k deploy/k8s/overlays/local
   kubectl rollout restart deploy/campanhas-api deploy/web -n conexao
   ```
4. Reiniciar a `identity-api` para assinar com a chave privada nova:
   ```bash
   kubectl rollout restart deploy/identity-api -n conexao
   kubectl rollout status deploy/identity-api -n conexao
   ```
5. Verificar: login novo funciona e chamada a `/api/v1/doacoes` com o token novo não responde 401.
6. Apagar os arquivos `.pem` locais.

## Observações

- Na Azure (opcional), as chaves ficam no Key Vault; trocar o segredo no cofre e reiniciar os pods na mesma ordem.
- Para rotação sem novo login, a validação precisaria aceitar duas chaves públicas (fora do escopo do MVP).
