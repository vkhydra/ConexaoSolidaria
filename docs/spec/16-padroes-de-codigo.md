# 16 — Padrões de código

[← Índice](../SPEC.md) · Anterior: [15 — Testes](15-testes.md) · Próximo: [17 — Git e commits](17-git-e-commits.md)

Referências: `wiki:disciplinas/solid`, `wiki:boas-praticas/solid`, `wiki:boas-praticas/api-dotnet`, `wiki:boas-praticas/efcore`, `wiki:boas-praticas/microsservicos`.

## 16.1 Arquivos de configuração da solução (Etapa 3)

| Arquivo | Conteúdo |
|---|---|
| `global.json` | SDK 10.0.x com `rollForward: latestFeature` |
| `Directory.Build.props` | `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true`, `GenerateDocumentationFile true` (com `NoWarn` 1591 nos testes), `RestorePackagesWithLockFile true`, `InvariantGlobalization false` |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally true`; todas as versões fixas aqui |
| `.editorconfig` | Estilo C# (namespaces por arquivo, `var` quando o tipo é evidente, chaves sempre, nomes `_camelCase` para campos privados), severidades dos analisadores |
| `.gitattributes` | `* text=auto eol=lf`; `*.ps1 eol=crlf`; binários marcados |
| `.config/dotnet-tools.json` | `dotnet-ef`, `husky`, `dotnet-reportgenerator-globaltool` |

## 16.2 Pacotes aprovados

| Finalidade | Pacote |
|---|---|
| Persistência relacional | `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Dapper` |
| Mensageria | `MassTransit.RabbitMQ`, `MassTransit.EntityFrameworkCore` (8.x) |
| NoSQL e busca | `MongoDB.Driver`, `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Elastic.Clients.Elasticsearch` |
| Segurança | `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next` |
| Validação | `FluentValidation` |
| API | `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI` |
| Resiliência | `Microsoft.Extensions.Http.Resilience` |
| Observabilidade | `Serilog.AspNetCore`, `prometheus-net.AspNetCore`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.*`, `Npgsql.OpenTelemetry`, `AspNetCore.HealthChecks.*` |
| Serverless | `Microsoft.Azure.Functions.Worker`, `Microsoft.Azure.Functions.Worker.Extensions.RabbitMQ` |
| IA (bônus) | `Microsoft.SemanticKernel` + conector Ollama |
| Testes | `xunit.v3`, `Shouldly`, `NSubstitute`, `Testcontainers.*`, `Microsoft.AspNetCore.Mvc.Testing`, `Reqnroll.xUnit`, `bunit`, `NetArchTest.Rules`, `Respawn`, `coverlet.collector`, `Microsoft.Extensions.TimeProvider.Testing` |

**Não usar:** MediatR, AutoMapper, FluentAssertions (licenças comerciais), MassTransit 9+, NEST (descontinuado), Moq. Novo pacote exige justificativa no PR (`➕ build: ...`).

## 16.3 SOLID na prática

| Princípio | Regra no projeto | Exemplo |
|---|---|---|
| SRP | Um caso de uso por classe; endpoint só traduz HTTP ↔ caso de uso | `ReceberDoacao` não sabe de HTTP nem de RabbitMQ |
| OCP | Pontos de variação por interface + nova implementação | `INotificadorRecibo` com implementação em log; e-mail real seria outra classe |
| LSP | Implementações respeitam o contrato da interface | Repositórios fake dos testes se comportam como os reais (mesmos testes de contrato) |
| ISP | Interfaces pequenas, por cliente | `ICampanhaLeitura` (painel, busca) separada de `ICampanhaRepositorio` (escrita) |
| DIP | Domínio e aplicação definem interfaces; infraestrutura implementa | `IPublicadorEventos` em Application, implementado com MassTransit em Infrastructure |

## 16.4 Regras de código

### Domínio
- Entidades com construtor privado e fábricas estáticas; setters privados.
- Invariantes verificadas na raiz do agregado; coleções expostas como `IReadOnlyCollection`.
- Erros de negócio como `Result`/`Error` com `code` estável; exceções só para falhas inesperadas.
- Sem atributos de EF Core ou de serialização no domínio.

### Aplicação
- Casos de uso com `ExecutarAsync(request, CancellationToken)`.
- DTOs `record` separados para entrada e saída; mapeamento manual (`ToResponse()`).
- Validação de formato com FluentValidation antes do domínio.

### API
- `Program.cs` enxuto: `builder.AddServiceDefaults()`, registro por extensões (`AddCampanhasInfrastructure()`), endpoints em classes `*Endpoints` com `MapGroup`.
- `TypedResults` nos endpoints (documentação OpenAPI precisa).
- Middleware global de exceções (`IExceptionHandler`) → `ProblemDetails` com `traceId`.
- `CancellationToken` repassado em toda chamada de I/O.

### Infraestrutura
- `IEntityTypeConfiguration<T>` por entidade; precisão de decimais explícita.
- Leituras com `AsNoTracking()` e projeção; Dapper só em leituras críticas.
- `DbContext` Scoped; `IDbContextFactory` em consumidores e serviços em segundo plano.
- Clientes de Elasticsearch, MongoDB e Redis como singleton.

### Injeção de dependência
- Injeção por construtor (primary constructors), sem service locator.
- `ValidateScopes` e `ValidateOnBuild` ligados em desenvolvimento.
- Nunca injetar Scoped em Singleton.

### Assíncrono
- `async`/`await` de ponta a ponta; nunca `.Result` ou `.Wait()`.
- Sem `async void` (exceto handlers de eventos Blazor).

### Nomenclatura
- Código em português para termos de domínio (linguagem ubíqua) e termos técnicos consagrados em inglês (`Repository`, `Handler`, `Endpoint`, `Consumer`).
- Projetos `ConexaoSolidaria.*` para blocos compartilhados; `<Contexto>.<Camada>` para serviços.

## 16.5 Blazor

- Componentes pequenos, parâmetros de entrada e `EventCallback` de saída.
- Regras de negócio em serviços injetados, nunca no `.razor`.
- Formulários com `EditForm` + validação; antiforgery ligado.
- `@key` em listas; CSS isolation (`.razor.css`).
- Carregamento de dados em `OnInitializedAsync`; `IAsyncDisposable` para cancelar assinaturas.
- `HttpClient` tipado por API, com handler que anexa o JWT e política de resiliência.
- Acessibilidade: rótulos em todos os campos, contraste adequado, foco visível.

## 16.6 Revisão de código (checklist do PR)

- [ ] O PR tem até ~400 linhas alteradas e um objetivo só.
- [ ] Descrição com o que mudou, por quê, como testar e link da issue.
- [ ] Testes novos cobrem o comportamento alterado.
- [ ] Nenhuma regra de negócio em endpoint, consumidor ou componente.
- [ ] Nenhum dado pessoal em logs, eventos ou exceções.
- [ ] Nenhum segredo ou URL fixa no código.
- [ ] Migrations novas revisadas (sem perda de dados).
- [ ] Documentação e ADR atualizados se houver decisão nova.
- [ ] `/code-review` executado; apontamentos resolvidos ou justificados.
