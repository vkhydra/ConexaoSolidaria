# Constituição do Conexão Solidária

[← README](../README.md)

**Versão:** 1.0.1 · **Ratificada em:** 2026-10-30 · **Última alteração:** 2026-10-30

Princípios inegociáveis do projeto. Valem para todo código, spec, teste, manifesto e documento. O comando `/plan` verifica cada princípio (seção "Verificação da constituição" do `plan.md`), e o `/analyze` e o agente `reviewer` apontam violações. Em conflito entre este arquivo e qualquer outro documento, vale este arquivo.

Palavras-chave: **DEVE** e **NÃO DEVE** são obrigatórias; **DEVERIA** admite exceção justificada no PR.

---

## I. Especificação antes do código

- Toda funcionalidade **DEVE** começar por `/specify` em `specs/features/NNN-nome/spec.md`, com critérios de aceitação em Gherkin.
- A spec **DEVE** dizer o quê e por quê; o como fica no `plan.md`.
- Nenhuma tarefa de implementação **DEVE** existir sem rastreio: RF → `spec.md` → `tasks.md` → commit → teste.
- Ambiguidade marcada como `[PRECISA ESCLARECER]` **DEVE** ser resolvida (`/clarify`) antes do `/plan`.

**Verificação:** `/analyze` sem pendências críticas antes do `/implement`.

## II. Domínio no centro

- O domínio segue DDD: agregados, value objects e eventos de domínio na linguagem ubíqua ([glossário](../docs/glossary.md)).
- Projetos `*.Domain` **NÃO DEVEM** depender de EF Core, ASP.NET, MassTransit ou qualquer pacote de infraestrutura.
- Dependências apontam para dentro: `Api` → `Application` → `Domain`; `Infrastructure` implementa interfaces de `Application`.
- Erros de negócio **DEVEM** ser `Result` com código estável (ex.: `campanha.meta_invalida`), nunca exceções.
- Bounded contexts **NÃO DEVEM** se referenciar; a comunicação é só por JWT e eventos de integração.

**Verificação:** teste de arquitetura (NetArchTest) em cada projeto de testes de unidade.

## III. Testes primeiro

- TDD (vermelho → verde → refatorar) **DEVE** ser usado em agregados, value objects e casos de uso.
- O teste e a implementação mínima **DEVEM** ir no mesmo commit; a refatoração vai em commit separado.
- Todo critério de aceitação em Gherkin **DEVE** virar um cenário automatizado (Reqnroll).
- Todo bug corrigido **DEVE** ganhar um teste que o reproduz, no mesmo commit.
- Testes de integração **DEVEM** usar infraestrutura real em containers (Testcontainers), não mocks de banco ou broker.

**Verificação:** CI executa unidade, contrato, integração e aceitação.

## IV. Cobertura mínima de 90%

- A cobertura **DEVE** ser de pelo menos **90% de linhas e 90% de ramos**, no total e em cada projeto de `src/`.
- `[ExcludeFromCodeCoverage]` **NÃO DEVE** ser usado para fugir da meta; só com `Justification` aprovada no PR.

**Verificação:** `scripts/verificar-cobertura.ps1 -Minimo 90` no hook `pre-push` e no CI ([estratégia de testes](../docs/testes.md)).

## V. Código documentado

- Todo tipo e membro C#, de qualquer visibilidade e inclusive nos testes, **DEVE** ter `/// <summary>` em português que explique o propósito, sem repetir o nome.
- `<param>`, `<returns>` e `<exception>` **DEVEM** acompanhar métodos com parâmetros, retorno ou exceções; implementações usam `<inheritdoc/>`.

**Verificação:** `GenerateDocumentationFile` + `TreatWarningsAsErrors` + StyleCop (regras `SA16xx`) quebram o build ([padrões de código](../docs/padroes-de-codigo.md)).

## VI. Histórico limpo e sempre compilável

- Todo commit **DEVE** compilar (`dotnet build -warnaserror`) e passar nos testes.
- Mensagem **DEVE** seguir `<gitmoji> <tipo>(<escopo>): <descrição>` em português ([git e commits](../docs/git-e-commits.md)).
- Cada arquivo criado **DEVE** ter o próprio commit, exceto quando só compila junto com outro (menor conjunto que compila).
- Commits **NÃO DEVEM** conter linha de coautoria (`Co-Authored-By`).
- `--no-verify` e `push --force` na `main` **NÃO DEVEM** ser usados.

**Verificação:** hooks do Husky.Net, `git rebase --exec` antes do PR e CI obrigatório na `main`.

## VII. Segurança e privacidade por padrão

- Endpoints **DEVEM** nascer protegidos (`FallbackPolicy`); público só com `AllowAnonymous()` explícito.
- Dados pessoais (nome, e-mail, CPF) **NÃO DEVEM** aparecer em logs, métricas, traces, eventos ou mensagens de erro; só IDs.
- Segredos **NÃO DEVEM** estar no código, em YAML versionado, em imagens ou em prompts; vêm de variáveis de ambiente e Secrets.
- Senha só como hash BCrypt; CPF cifrado (AES-GCM) com hash HMAC para unicidade.
- Coleta mínima: só os dados necessários às funcionalidades especificadas ([segurança e LGPD](product/seguranca-e-lgpd.md)).

**Verificação:** `/security-review`, gitleaks, Trivy, CodeQL e checklist OWASP.

## VIII. Consistência por eventos

- Mudança de estado que outro processo precisa conhecer **DEVE** publicar evento pelo Transactional Outbox, na mesma transação dos dados.
- Todo consumidor **DEVE** ser idempotente (entrega at-least-once).
- Contratos de evento **DEVEM** ser versionados (`V1`, `V2`), só com campos adicionados no fim, e protegidos por teste de contrato.
- O total arrecadado **DEVE** ser escrito só pelo `Arrecadacao.Worker` ([ADR 0004](../docs/adr/0004-worker-unico-escritor-do-total.md)).

**Verificação:** testes de reentrega e de snapshot dos contratos ([API e eventos](product/api-e-eventos.md)).

## IX. Observabilidade desde o primeiro endpoint

- Todo serviço **DEVE** expor `/health/live`, `/health/ready` e `/metrics`.
- Logs **DEVEM** ser estruturados (JSON) com `traceId`; traces **DEVEM** propagar `traceparent` por HTTP e mensagens.
- Toda funcionalidade de negócio **DEVERIA** ter métrica própria (contador ou histograma) com labels de baixa cardinalidade.

**Verificação:** item da DoD; dashboard Grafana com dados reais ([observabilidade](product/observabilidade.md)).

## X. Infraestrutura como código e imutável

- Toda infraestrutura **DEVE** estar versionada (Dockerfile, docker-compose, Kustomize, Helm values, Bicep).
- Imagens **DEVEM** usar tag imutável (`sha-<commit>`), usuário não root e base sem SDK.
- Configuração **DEVE** vir de variáveis de ambiente (12-factor).

**Verificação:** `kubectl apply -k ... --dry-run=server` e Trivy no CI ([infraestrutura](product/infraestrutura.md)).

## XI. Simplicidade e licenças livres

- Só o necessário para o requisito atual (YAGNI); abstração **DEVE** ter mais de um uso real.
- Pacotes com licença comercial (MediatR, AutoMapper, FluentAssertions, MassTransit 9+, Moq) **NÃO DEVEM** ser usados ([ADR 0013](../docs/adr/0013-bibliotecas-sem-licenca-comercial.md)).
- Toda decisão arquitetural nova **DEVE** ter ADR em `docs/adr/` antes do código.

**Verificação:** checklist do PR e `/code-review`.

## XII. Português e fontes de referência

- Código de domínio, specs, documentação, commits e interface **DEVEM** estar em português do Brasil; termos técnicos consagrados podem ficar em inglês (`Repository`, `Handler`, `Consumer`).
- A LLMWiki (`C:\Victor\Fiap\LLMWiki\wiki`) é a referência de boas práticas e **NÃO DEVE** ser alterada (somente leitura).
- Documentação oficial e atual (MCP Microsoft Learn) **DEVERIA** ser consultada antes de usar uma API nova.

---

## Restrições técnicas

| Item | Restrição |
|---|---|
| Plataforma | .NET 10 LTS, C# com `Nullable` ligado, `.slnx` ([ADR 0001](../docs/adr/0001-escolha-da-stack.md)) |
| Ambiente oficial | Kubernetes local (Minikube); Azure só opcional ([ADR 0015](../docs/adr/0015-execucao-local-com-cloud-opcional.md)) |
| Sistemas de desenvolvimento | Windows, macOS e Linux, com scripts em PowerShell 7 ([pré-requisitos](../docs/pre-requisitos.md)) |
| Metas de qualidade | Requisitos não funcionais e SLOs em [requisitos não funcionais](product/requisitos-nao-funcionais.md) |

## Fluxo de desenvolvimento

```text
/constitution → /specify → /clarify → /plan → /tasks → /analyze → /implement → /code-review → PR
```

| Portão | Quando | Critério |
|---|---|---|
| Spec pronta | Antes do `/plan` | Nenhum `[PRECISA ESCLARECER]`; critérios de aceitação testáveis (DoR) |
| Plano aprovado | Antes do `/tasks` | Todos os princípios marcados como atendidos ou com exceção justificada |
| Análise limpa | Antes do `/implement` | `/analyze` sem achados críticos |
| Pronto | Antes do merge | Definition of Done ([processo ágil](../docs/processo-agil.md)) e CI verde |

## Governança

1. Esta constituição prevalece sobre as demais práticas do projeto.
2. Alterações são feitas pelo comando `/constitution`, num PR próprio, com aprovação de pelo menos um integrante além do autor.
3. Versionamento semântico:
   - **MAJOR:** remover ou redefinir um princípio de forma incompatível.
   - **MINOR:** acrescentar princípio ou seção, ou ampliar uma regra.
   - **PATCH:** esclarecimento de texto sem mudar a regra.
4. Toda alteração atualiza a versão, a data e os documentos afetados (specs, comandos, agentes, skills, `CLAUDE.md`).
5. Exceção a um princípio exige justificativa na seção "Verificação da constituição" do `plan.md` e aprovação no PR.
