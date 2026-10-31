# 16 — Padrões de código

[← README](../../README.md) · Anterior: [15 — Testes](../testes.md) · Próximo: [17 — Git e commits](17-git-e-commits.md)

Referências: `wiki:disciplinas/solid`, `wiki:boas-praticas/solid`, `wiki:boas-praticas/api-dotnet`, `wiki:boas-praticas/efcore`, `wiki:boas-praticas/microsservicos`.

## 16.1 Arquivos de configuração da solução (Etapa 3)

| Arquivo | Conteúdo |
|---|---|
| `global.json` | SDK 10.0.x com `rollForward: latestFeature` |
| `Directory.Build.props` | `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true`, `GenerateDocumentationFile true` (em **todos** os projetos, inclusive testes; ver 16.7), `RestorePackagesWithLockFile true`, `InvariantGlobalization false` |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally true`; todas as versões fixas aqui |
| `.editorconfig` | Estilo C# (namespaces por arquivo, `var` quando o tipo é evidente, chaves sempre, nomes `_camelCase` para campos privados), severidades dos analisadores |
| `.gitattributes` | `* text=auto eol=lf`; `*.ps1 eol=crlf`; binários marcados |
| `coverlet.runsettings` | Filtros e exclusões da cobertura ([15.7](../testes.md)) |
| `stylecop.json` | Regras de documentação XML do StyleCop ([16.7](#167-documentação-xml-summary)) |
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
| Análise estática | `StyleCop.Analyzers` (MIT; só as regras de documentação ligadas, ver 16.7) |
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
- [ ] Cobertura continua ≥ 90% de linhas e ramos (total e por projeto).
- [ ] Todo tipo e membro novo tem `/// <summary>` útil (não repete o nome).
- [ ] Nenhuma regra de negócio em endpoint, consumidor ou componente.
- [ ] Nenhum dado pessoal em logs, eventos ou exceções.
- [ ] Nenhum segredo ou URL fixa no código.
- [ ] Migrations novas revisadas (sem perda de dados).
- [ ] Documentação e ADR atualizados se houver decisão nova.
- [ ] `/code-review` executado; apontamentos resolvidos ou justificados.

## 16.7 Documentação XML (`<summary>`)

Todo código C# do repositório é documentado com comentários XML (`///`), em português.

### Abrangência

| Elemento | Exige `<summary>` |
|---|---|
| Classes, records, structs, interfaces, enums e delegates | Sim, de qualquer visibilidade (`public`, `internal`, `private`, `protected`) |
| Métodos, construtores, propriedades, eventos e indexadores | Sim, de qualquer visibilidade |
| Campos (inclusive privados) e constantes | Sim |
| Itens de enum | Sim |
| Projetos de teste (classes e métodos de teste) | Sim: o `<summary>` descreve o cenário em Dado/Quando/Então |
| Componentes Blazor | Sim, no code-behind `.razor.cs` (classe `partial`) e em cada `[Parameter]` |
| Código gerado (`Migrations`, `*.g.cs`, arquivos do Reqnroll) | Não |

### Tags

| Tag | Quando usar |
|---|---|
| `<summary>` | Sempre. Uma ou duas frases dizendo **o que** o elemento faz ou representa no domínio |
| `<param name="...">` | Cada parâmetro de método, construtor e record posicional |
| `<typeparam name="...">` | Cada parâmetro genérico |
| `<returns>` | Métodos que retornam valor (inclusive `Task<T>`), descrevendo também os erros possíveis do `Result` |
| `<exception cref="...">` | Cada exceção lançada de propósito |
| `<remarks>` | Regra de negócio, decisão de projeto ou referência a ADR/RF |
| `<see cref="..."/>` | Referência a outro tipo ou membro (verificada pelo compilador) |
| `<inheritdoc/>` | Implementações de interface e `override`: a documentação fica na interface ou na classe base |
| `<example>` | Opcional, para APIs públicas pouco óbvias |

### Regras de escrita

- Descrever o propósito e as regras, não repetir o nome (`/// <summary>Obtém o id.</summary>` não serve).
- Citar o código de erro e o RF quando houver (ex.: `campanha.meta_invalida`, RF02).
- Frases completas, terminadas em ponto.
- Nunca colocar dados pessoais reais, segredos ou URLs internas nos comentários.
- Comentário desatualizado é defeito: alterar o comportamento exige alterar o `<summary>` no mesmo commit.

### Exemplos

```csharp
/// <summary>
/// Campanha de arrecadação da ONG. Raiz do agregado de Arrecadação.
/// </summary>
/// <remarks>
/// O total arrecadado só muda por <see cref="RegistrarDoacaoProcessada(Dinheiro)"/>,
/// chamado pelo Worker depois de processar a doação (RF05).
/// </remarks>
public sealed class Campanha
{
    /// <summary>
    /// Cria uma campanha com status <see cref="StatusCampanha.Ativa"/> e total arrecadado zero.
    /// </summary>
    /// <param name="titulo">Título exibido no painel, de 3 a 120 caracteres.</param>
    /// <param name="descricao">Descrição da campanha, até 2000 caracteres.</param>
    /// <param name="periodo">Período em que a campanha aceita doações.</param>
    /// <param name="meta">Meta financeira; deve ser maior que zero.</param>
    /// <param name="hoje">Data atual, obtida do <see cref="TimeProvider"/>.</param>
    /// <returns>
    /// A campanha criada, ou o erro <c>campanha.data_fim_passada</c> ou <c>campanha.meta_invalida</c>.
    /// </returns>
    public static Result<Campanha> Criar(
        string titulo, string descricao, Periodo periodo, Dinheiro meta, DateOnly hoje)
    {
        // ...
    }
}
```

```csharp
/// <summary>
/// Testes das regras de criação do agregado <see cref="Campanha"/>.
/// </summary>
public sealed class CampanhaCriarTests
{
    /// <summary>
    /// Dado uma data fim anterior a hoje, quando a campanha é criada,
    /// então retorna o erro <c>campanha.data_fim_passada</c>.
    /// </summary>
    [Fact]
    public void Criar_DataFimNoPassado_RetornaErroDataFimPassada()
    {
        // ...
    }
}
```

### Como é garantido

| Camada | Mecanismo | Efeito |
|---|---|---|
| Compilador | `GenerateDocumentationFile true` + `TreatWarningsAsErrors true` em todos os projetos | Falta de documentação em membro visível (CS1591) e `cref` inválido (CS1574) quebram o build |
| StyleCop.Analyzers | `stylecop.json` com `documentPrivateElements`, `documentPrivateFields`, `documentInternalElements` e `documentInterfaces` ligados | Estende a exigência para membros `internal` e `private` |
| `.editorconfig` | `SA1600`, `SA1601`, `SA1602`, `SA1604`, `SA1606`, `SA1611`, `SA1614`, `SA1615`, `SA1616`, `SA1618` e `SA1622` com `severity = error`; demais regras `SA*` desligadas; `[**/Migrations/*.cs] generated_code = true` | Só as regras de documentação são cobradas |
| Hook `pre-commit` e CI | `dotnet build -warnaserror` | Commit e PR sem `<summary>` não passam |
| OpenAPI | Gerador de OpenAPI do .NET 10 lê os comentários XML dos endpoints e DTOs | O `<summary>` vira a descrição no Swagger |
| Revisão | Checklist do PR (16.6) | Avalia se o texto é útil, o que o compilador não verifica |

`stylecop.json` (raiz, referenciado como `AdditionalFiles` no `Directory.Build.props`):
```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "documentExposedElements": true,
      "documentInternalElements": true,
      "documentPrivateElements": true,
      "documentPrivateFields": true,
      "documentInterfaces": true,
      "xmlHeader": false
    }
  }
}
```
