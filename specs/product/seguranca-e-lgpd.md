# 11 — Segurança e LGPD

[← README](../../README.md)

Referências: `wiki:disciplinas/lgpd`, `wiki:boas-praticas/lgpd`, `wiki:conceitos/owasp-top-10`, `wiki:conceitos/seguranca-em-microsservicos`, `wiki:conceitos/seguranca-de-containers`.

> As decisões de LGPD aqui são técnicas e de projeto acadêmico; não são orientação jurídica.

## 11.1 Autenticação

| Item | Definição |
|---|---|
| Hash de senha | BCrypt (`BCrypt.Net-Next`), work factor 12 |
| Política de senha | 8–64 caracteres, letra, número e caractere especial |
| Token | JWT RS256, 60 min, sem refresh token |
| Chaves | RSA 2048; privada em Secret (só Identity), pública em ConfigMap |
| Validação | `iss`, `aud`, `exp`, `nbf`, assinatura; `ClockSkew` de 30 s |
| Força bruta | 5 falhas em 15 min bloqueiam o e-mail por 15 min; rate limit de 10 req/min no login (Kong) |
| Mensagens | Genéricas no login (não revela se o e-mail existe) |
| Usuário anonimizado | Token rejeitado: Campanhas.Api não consulta Identity, então o bloqueio vale no login e no `/doadores/me`. Tokens já emitidos expiram em até 60 min (risco aceito e documentado) |

## 11.2 Autorização

| Policy | Requisito | Endpoints |
|---|---|---|
| `GestorONG` | Claim `role = GestorONG` | `/campanhas/**` (exceto busca), `/admin/**` |
| `Doador` | Claim `role = Doador` | `/doacoes`, `/doadores/me` |
| Pública | Nenhum | `/doadores` (POST), `/auth/login`, `/transparencia/**`, `/campanhas/busca` |

- Autorização sempre no serviço (`RequireAuthorization("GestorONG")`); o gateway não substitui essa checagem.
- `FallbackPolicy` exige autenticação: endpoint novo nasce protegido; público só com `AllowAnonymous()` explícito.
- No Blazor, `<AuthorizeView>` é só experiência de uso; a API valida sempre.

## 11.3 Proteção de dados pessoais

| Dado | Classificação | Armazenamento | Exibição | Logs/eventos |
|---|---|---|---|---|
| Nome completo | Pessoal | Texto | Só para o próprio titular | Nunca |
| E-mail | Pessoal | Texto normalizado | Só para o próprio titular | Nunca |
| CPF | Pessoal (identificador) | AES-256-GCM + HMAC-SHA256 | Mascarado `***.456.789-**` | Nunca |
| Senha | Credencial | Hash BCrypt | Nunca | Nunca |
| ID do usuário | Pseudônimo | UUID | — | Sim |
| Valor da doação | Não pessoal se separado do titular | `numeric` | Painel agregado | Sim |

### Cifragem do CPF
- `cpf_cifrado = versãoChave (1 byte) || nonce (12) || tag (16) || cifra` com `System.Security.Cryptography.AesGcm`.
- `cpf_hash = HMAC-SHA256(chaveHash, cpfSomenteDigitos)` para verificar unicidade sem decifrar.
- Chaves de 256 bits em Secret (`Cpf__ChaveCifra`, `Cpf__ChaveHash`), geradas por `scripts/k8s-secrets.ps1`.
- Rotação: nova versão de chave; leitura aceita as versões antigas; reescrita gradual.

## 11.4 LGPD

### Papéis
| Papel | Quem |
|---|---|
| Controlador | ONG Esperança Solidária |
| Operador | Equipe que mantém a plataforma (o grupo) e provedores de infraestrutura |
| Encarregado (DPO) | Contato indicado no README e na política de privacidade (fictício no projeto) |

### Registro das operações de tratamento (`docs/lgpd/registro-de-tratamento.md`)
| Finalidade | Dados | Base legal (art. 7º) | Retenção |
|---|---|---|---|
| Cadastro e autenticação do doador | Nome, e-mail, senha (hash) | Execução de contrato (inciso V) | Até a anonimização a pedido |
| Identificação fiscal do doador | CPF | Consentimento (inciso I) no aceite da política; revisar se houver obrigação legal (inciso II) | Até a anonimização |
| Prestação de contas das doações | ID pseudônimo, valor, data | Legítimo interesse (inciso IX), com teste de balanceamento documentado | Indefinida, sem dados pessoais após anonimização |
| Segurança (logs e métricas) | IDs, IP no gateway | Legítimo interesse | 7 dias |

### Princípios aplicados
| Princípio | Aplicação |
|---|---|
| Finalidade e necessidade (minimização) | Só os campos do enunciado; eventos e logs com IDs |
| Transparência | Política de privacidade versionada (`docs/lgpd/politica-de-privacidade.md`) exibida no cadastro |
| Segurança | Seções 11.1 a 11.6 |
| Privacidade por design e por padrão | Endpoints protegidos por padrão; CPF mascarado por padrão; Swagger desligado fora do local |
| Livre acesso e eliminação | RF08 (`GET` e `DELETE /doadores/me`) |
| Responsabilização | ADRs, registro de tratamento e logs de auditoria |

### Anonimização × pseudonimização
- O livro de doações guarda `doadorId` (pseudônimo). Enquanto o cadastro existe, o ID liga a doação à pessoa.
- Após a anonimização do cadastro, o ID não aponta mais para nenhum dado pessoal: o registro passa a ser anônimo.

### IA (bônus) e LGPD
- O assistente de descrição de campanha recebe só título e tópicos digitados pelo gestor; nenhum dado de doador.
- Modelo local (Ollama): nada sai da máquina.
- A sugestão é sempre revisada e editada por uma pessoa antes de publicar; a UI informa que o texto foi gerado por IA.

## 11.5 Segredos (Zero Hardcoded Credentials)

| Onde | Como |
|---|---|
| Desenvolvimento | `dotnet user-secrets` ou `.env` (ignorado pelo git); modelo em `.env.example` sem valores reais |
| docker-compose | `env_file: .env` |
| Kubernetes local | Secrets criados por `scripts/k8s-secrets.ps1` a partir do `.env`; versionados só os `*.example.yaml` |
| Azure (opcional) | Key Vault + CSI Secrets Store Driver + Workload Identity |
| CI | GitHub Secrets por environment; `GITHUB_TOKEN` com permissões mínimas; OIDC para a Azure |
| Detecção | gitleaks no CI e GitHub secret scanning + push protection ligados no repositório |

## 11.6 Segurança da plataforma

| Camada | Medida |
|---|---|
| Gateway | HTTPS (mkcert local), rate limiting, CORS só para o domínio do Web, limite de corpo de 1 MB, cabeçalhos de segurança (`X-Content-Type-Options`, `Strict-Transport-Security`) |
| APIs | Validação de entrada; consultas parametrizadas (EF Core e Dapper); `ProblemDetails` sem stack trace fora do desenvolvimento; antiforgery nos formulários Blazor |
| Imagens | Base `mcr.microsoft.com/dotnet/aspnet:10.0` (ou `-noble-chiseled`), usuário não root, sem SDK na imagem final, Trivy no CI |
| Pods | `runAsNonRoot`, `readOnlyRootFilesystem`, `allowPrivilegeEscalation: false`, `capabilities.drop: [ALL]`, `seccompProfile: RuntimeDefault`; namespaces com Pod Security Standard `restricted` |
| Rede | `NetworkPolicy`: `infra` aceita conexões só de `conexao`; bancos sem Service externo |
| RBAC K8s | ServiceAccount por aplicação, `automountServiceAccountToken: false` |
| Dependências | Dependabot; `dotnet list package --vulnerable` no CI |
| Código | CodeQL (C#) no GitHub; analisadores .NET com avisos como erro |
| Admin | Admin API do Kong, painel do RabbitMQ, Grafana e Elasticsearch acessíveis só por `kubectl port-forward` |

## 11.7 Checklist OWASP Top 10 (revisão na Etapa 16)

| Risco | Mitigação no projeto |
|---|---|
| A01 Quebra de controle de acesso | Policies por role, `FallbackPolicy`, testes de 401/403 |
| A02 Falhas criptográficas | BCrypt, AES-GCM, TLS, chaves em Secret |
| A03 Injeção | EF Core e Dapper parametrizados; validação de entrada |
| A04 Design inseguro | Modelo de ameaças simples no ADR; regras no domínio |
| A05 Configuração incorreta | Swagger só local; cabeçalhos de segurança; Pod Security |
| A06 Componentes vulneráveis | Dependabot, Trivy, `--vulnerable` |
| A07 Falhas de autenticação | Bloqueio por tentativas, mensagens genéricas, JWT validado |
| A08 Falhas de integridade | Actions fixadas por SHA; imagens por tag imutável |
| A09 Falhas de log e monitoramento | Logs estruturados, alertas, auditoria sem dados pessoais |
| A10 SSRF | Nenhuma URL fornecida pelo usuário é chamada pelo servidor |
