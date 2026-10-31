using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Campanhas.Infrastructure.Persistencia;
using ConexaoSolidaria.Contracts.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ConexaoSolidaria.Bdd.Suporte;

/// <summary>
/// Ambiente de um cenário para a Campanhas.Api real em memória, com banco PostgreSQL exclusivo, a data de "hoje" controlada
/// e a validação do JWT emitido pela Identity do mesmo cenário.
/// </summary>
/// <param name="identidade">Ambiente da Identity, que emite os tokens.</param>
/// <param name="respostaDoCenario">Guarda a última resposta do cenário, compartilhada com a Identity.</param>
public sealed class AmbienteCampanhas(AmbienteIdentity identidade, RespostaDoCenario respostaDoCenario) : IDisposable
{
    /// <summary>
    /// Relógio do cenário, fixado em "hoje" e usado pela API.
    /// </summary>
    private readonly RelogioDoCenario _relogio = new();

    /// <summary>
    /// Versões (ETag) guardadas pelo cenário, por rótulo ("1", "2").
    /// </summary>
    private readonly Dictionary<string, string> _versoes = [];

    /// <summary>
    /// Identificadores das campanhas criadas pelo cenário, por título.
    /// </summary>
    private readonly Dictionary<string, Guid> _idsPorTitulo = [];

    /// <summary>
    /// String de conexão do banco do cenário.
    /// </summary>
    private string? _conexao;

    /// <summary>
    /// API iniciada pelo cenário, descartada no fim.
    /// </summary>
    private WebApplicationFactory<Program>? _api;

    /// <summary>
    /// Cliente HTTP da API.
    /// </summary>
    private HttpClient? _cliente;

    /// <summary>
    /// Obtém ou define se o Redis do cenário deve apontar para um endereço sem servidor, simulando o Redis fora do ar.
    /// Precisa ser definido antes da primeira chamada à API.
    /// </summary>
    public bool RedisForaDoAr { get; set; }

    /// <summary>
    /// Obtém o identificador da última campanha criada pelo cenário.
    /// </summary>
    public Guid? UltimaCampanhaId { get; private set; }

    /// <summary>
    /// Obtém o identificador da última doação aceita (202) pela API no cenário.
    /// </summary>
    public Guid? UltimaDoacaoId { get; private set; }

    /// <summary>
    /// Obtém a string de conexão do banco do cenário; só vale depois de <see cref="GarantirIniciadaAsync"/>.
    /// </summary>
    public string Conexao => _conexao ?? throw new InvalidOperationException("A API do cenário ainda não foi iniciada.");

    /// <summary>
    /// Inicia a API e o banco do cenário, se ainda não foram iniciados.
    /// </summary>
    /// <returns>Tarefa que termina com a API iniciada.</returns>
    public async Task GarantirIniciadaAsync()
    {
        await ObterClienteAsync();
    }

    /// <summary>
    /// Obtém o identificador de uma campanha do cenário pelo título.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <returns>O identificador da campanha.</returns>
    public Guid IdDaCampanha(string titulo)
    {
        return _idsPorTitulo[titulo];
    }

    /// <summary>
    /// Doa para a última campanha do cenário com o token em uso e guarda a resposta; se for aceita, guarda o identificador da doação.
    /// </summary>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <param name="chaveDeIdempotencia">Valor do cabeçalho <c>Idempotency-Key</c>, ou nulo para não enviar.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task DoarAsync(string valor, string? chaveDeIdempotencia = null)
    {
        await DoarParaAsync(UltimaCampanhaId!.Value, valor, chaveDeIdempotencia);
    }

    /// <summary>
    /// Doa para a campanha informada com o token em uso e guarda a resposta; se for aceita, guarda o identificador da doação.
    /// </summary>
    /// <param name="campanhaId">Campanha da doação.</param>
    /// <param name="valor">Valor da doação, com ponto decimal.</param>
    /// <param name="chaveDeIdempotencia">Valor do cabeçalho <c>Idempotency-Key</c>, ou nulo para não enviar.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task DoarParaAsync(Guid campanhaId, string valor, string? chaveDeIdempotencia = null)
    {
        var cliente = await ObterClienteAsync();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/doacoes")
        {
            Content = JsonContent.Create(new { idCampanha = campanhaId, valorDoacao = decimal.Parse(valor, CultureInfo.InvariantCulture) }),
        };
        if (chaveDeIdempotencia is not null)
        {
            requisicao.Headers.Add("Idempotency-Key", chaveDeIdempotencia);
        }

        respostaDoCenario.Resposta = await cliente.SendAsync(requisicao);
        if (respostaDoCenario.Resposta.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            var corpo = JsonDocument.Parse(await respostaDoCenario.Resposta.Content.ReadAsStringAsync()).RootElement;
            UltimaDoacaoId = corpo.GetProperty("doacaoId").GetGuid();
        }
    }

    /// <summary>
    /// Lê as doações recebidas que a API gravou no Outbox, na ordem de gravação.
    /// </summary>
    /// <returns>Os eventos <c>DoacaoRecebidaEvent</c> gravados.</returns>
    public async Task<IReadOnlyList<DoacaoRecebidaEvent>> LerDoacoesDoOutboxAsync()
    {
        await ObterClienteAsync();
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT body FROM outbox_message WHERE body LIKE '%doacaoId%' ORDER BY sequence_number", conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        var doacoes = new List<DoacaoRecebidaEvent>();
        while (await leitor.ReadAsync())
        {
            var doacao = AmbienteWorker.LerDoacao(leitor.GetString(0));
            if (doacao is not null)
            {
                doacoes.Add(doacao);
            }
        }

        return doacoes;
    }

    /// <summary>
    /// Lê o total arrecadado de uma campanha direto do banco.
    /// </summary>
    /// <param name="campanhaId">Identificador da campanha.</param>
    /// <returns>O total arrecadado.</returns>
    public async Task<decimal> LerTotalAsync(Guid campanhaId)
    {
        await ObterClienteAsync();
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT total_arrecadado FROM campanhas WHERE id = @id", conexao);
        comando.Parameters.AddWithValue("id", campanhaId);
        return (decimal)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Fixa a data de "hoje" em São Paulo (ao meio-dia) para a API do cenário.
    /// </summary>
    /// <param name="hoje">Data de hoje no formato <c>aaaa-MM-dd</c>.</param>
    public void DefinirHoje(string hoje)
    {
        var data = DateOnly.ParseExact(hoje, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        _relogio.Definir(new DateTimeOffset(data.Year, data.Month, data.Day, 15, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// Passa a enviar o token informado em todas as chamadas; nulo remove a autenticação.
    /// </summary>
    /// <param name="token">JWT, ou nulo para chamar sem token.</param>
    /// <returns>Tarefa que termina com o cabeçalho ajustado.</returns>
    public async Task UsarTokenAsync(string? token)
    {
        var cliente = await ObterClienteAsync();
        cliente.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Cria uma campanha e guarda a resposta; se der certo, guarda o identificador e a versão "1".
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="inicio">Data início (<c>aaaa-MM-dd</c>).</param>
    /// <param name="fim">Data fim (<c>aaaa-MM-dd</c>).</param>
    /// <param name="meta">Meta em reais, com ponto decimal.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task CriarAsync(string titulo, string inicio, string fim, string meta)
    {
        var cliente = await ObterClienteAsync();
        respostaDoCenario.Resposta = await cliente.PostAsJsonAsync("/api/v1/campanhas", Corpo(titulo, inicio, fim, meta));
        if (respostaDoCenario.Resposta.IsSuccessStatusCode)
        {
            var corpo = JsonDocument.Parse(await respostaDoCenario.Resposta.Content.ReadAsStringAsync()).RootElement;
            UltimaCampanhaId = corpo.GetProperty("id").GetGuid();
            _idsPorTitulo[titulo] = UltimaCampanhaId.Value;
            _versoes["1"] = respostaDoCenario.Resposta.Headers.ETag!.Tag;
        }
    }

    /// <summary>
    /// Altera a meta da última campanha informando a versão (rótulo) que o gestor viu, e guarda a resposta.
    /// Se der certo, guarda o novo ETag como a versão seguinte ("2").
    /// </summary>
    /// <param name="meta">Nova meta, com ponto decimal.</param>
    /// <param name="rotuloDaVersao">Rótulo da versão enviada no If-Match.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task AlterarMetaAsync(string meta, string rotuloDaVersao)
    {
        var cliente = await ObterClienteAsync();
        var atual = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/campanhas/{UltimaCampanhaId}");
        using var requisicao = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/campanhas/{UltimaCampanhaId}")
        {
            Content = JsonContent.Create(Corpo(
                atual.GetProperty("titulo").GetString()!,
                atual.GetProperty("dataInicio").GetString()!,
                atual.GetProperty("dataFim").GetString()!,
                meta)),
        };
        requisicao.Headers.TryAddWithoutValidation("If-Match", _versoes[rotuloDaVersao]);
        respostaDoCenario.Resposta = await cliente.SendAsync(requisicao);
        if (respostaDoCenario.Resposta.IsSuccessStatusCode)
        {
            _versoes["2"] = respostaDoCenario.Resposta.Headers.ETag!.Tag;
        }
    }

    /// <summary>
    /// Muda o status da última campanha e guarda a resposta.
    /// </summary>
    /// <param name="status">Status de destino.</param>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task AlterarStatusAsync(string status)
    {
        var cliente = await ObterClienteAsync();
        respostaDoCenario.Resposta = await cliente.PatchAsJsonAsync($"/api/v1/campanhas/{UltimaCampanhaId}/status", new { status });
    }

    /// <summary>
    /// Lista as campanhas da gestão com o token em uso e guarda a resposta.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task ListarAsync()
    {
        respostaDoCenario.Resposta = await (await ObterClienteAsync()).GetAsync("/api/v1/campanhas");
    }

    /// <summary>
    /// Lê um campo de texto do JSON da última resposta, convertendo números para texto com ponto decimal.
    /// </summary>
    /// <param name="campo">Nome do campo em camelCase.</param>
    /// <returns>O valor em texto, ou nulo se o campo não existe.</returns>
    public async Task<string?> LerCampoAsync(string campo)
    {
        var texto = await respostaDoCenario.Resposta!.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);
        return documento.RootElement.TryGetProperty(campo, out var valor)
            ? (valor.ValueKind == JsonValueKind.Number ? valor.GetDecimal().ToString("F2", CultureInfo.InvariantCulture) : valor.GetString())
            : null;
    }

    /// <summary>
    /// Insere uma campanha direto no banco, como dado já existente, sem passar pelas regras de criação da API (por exemplo,
    /// títulos curtos e datas no passado). Limpa o cache do painel.
    /// </summary>
    /// <param name="titulo">Título da campanha.</param>
    /// <param name="status">Status: <c>Ativa</c>, <c>Concluida</c> ou <c>Cancelada</c>.</param>
    /// <param name="inicio">Data início (<c>aaaa-MM-dd</c>).</param>
    /// <param name="fim">Data fim (<c>aaaa-MM-dd</c>).</param>
    /// <param name="meta">Meta em reais, com ponto decimal.</param>
    /// <param name="total">Total arrecadado em reais, com ponto decimal.</param>
    /// <returns>Tarefa que termina com a campanha inserida.</returns>
    public async Task InserirCampanhaAsync(string titulo, string status, string inicio, string fim, string meta, string total)
    {
        await ObterClienteAsync();
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO campanhas (id, titulo, descricao, data_inicio, data_fim, meta_financeira, total_arrecadado, status, criado_em, atualizado_em)
            VALUES (@id, @titulo, 'Campanha de teste.', @inicio, @fim, @meta, @total, @status, now(), now())
            """,
            conexao);
        var id = Guid.CreateVersion7();
        comando.Parameters.AddWithValue("id", id);
        comando.Parameters.AddWithValue("titulo", titulo);
        comando.Parameters.AddWithValue("inicio", DateOnly.ParseExact(inicio, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("fim", DateOnly.ParseExact(fim, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("meta", decimal.Parse(meta, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("total", decimal.Parse(total, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("status", status);
        await comando.ExecuteNonQueryAsync();
        _idsPorTitulo[titulo] = id;
        UltimaCampanhaId = id;
        if (!RedisForaDoAr)
        {
            await _api!.Services.GetRequiredService<Campanhas.Application.Transparencia.ICachePainel>().RemoverAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Consulta o painel público como visitante, sem token, e guarda a resposta.
    /// </summary>
    /// <returns>Tarefa que termina com a resposta guardada.</returns>
    public async Task ConsultarPainelAsync()
    {
        await ObterClienteAsync();
        using var visitante = _api!.CreateClient();
        respostaDoCenario.Resposta = await visitante.GetAsync("/api/v1/transparencia/campanhas");
    }

    /// <summary>
    /// Lê os itens do painel da última resposta.
    /// </summary>
    /// <returns>Os itens do painel, como elementos JSON.</returns>
    public async Task<IReadOnlyList<JsonElement>> LerPainelAsync()
    {
        var texto = await respostaDoCenario.Resposta!.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);
        return [.. documento.RootElement.EnumerateArray().Select(e => e.Clone())];
    }

    /// <summary>
    /// Libera a API e o cliente.
    /// </summary>
    public void Dispose()
    {
        _cliente?.Dispose();
        _api?.Dispose();
    }

    /// <summary>
    /// Monta o corpo de uma campanha.
    /// </summary>
    /// <param name="titulo">Título.</param>
    /// <param name="inicio">Data início.</param>
    /// <param name="fim">Data fim.</param>
    /// <param name="meta">Meta em reais, com ponto decimal.</param>
    /// <returns>O corpo da requisição.</returns>
    private static object Corpo(string titulo, string inicio, string fim, string meta)
    {
        return new
        {
            titulo,
            descricao = "Campanha de teste.",
            dataInicio = inicio,
            dataFim = fim,
            metaFinanceira = decimal.Parse(meta, CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Obtém o cliente da API, iniciando-a na primeira chamada.
    /// </summary>
    /// <returns>O cliente HTTP da API de Campanhas.</returns>
    private async Task<HttpClient> ObterClienteAsync()
    {
        if (_cliente is not null)
        {
            return _cliente;
        }

        var conexao = await ServidorPostgres.CriarBancoAsync();
        _conexao = conexao;
        var opcoes = new DbContextOptionsBuilder<CampanhasDbContext>();
        CampanhasDbContext.Configurar(opcoes, conexao);
        await using (var contexto = new CampanhasDbContext(opcoes.Options))
        {
            await contexto.Database.MigrateAsync();
        }

        _api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Campanhas", conexao);
            builder.UseSetting("ConnectionStrings:Redis", RedisForaDoAr ? "localhost:1,abortConnect=false,connectTimeout=300,syncTimeout=300" : ServidorRedis.Conexao);
            builder.UseSetting("RabbitMq:Host", "rabbitmq.invalido.local");
            builder.UseSetting("Jwt:ChavePublicaPem", identidade.ChavePublicaPem);
            builder.ConfigureServices(servicos =>
            {
                foreach (var descritor in servicos.Where(d => d.ServiceType == typeof(IHostedService) && (d.ImplementationType?.FullName?.Contains("MassTransit", StringComparison.Ordinal) ?? false)).ToList())
                {
                    servicos.Remove(descritor);
                }

                servicos.AddSingleton<TimeProvider>(_relogio);
            });
        });
        _cliente = _api.CreateClient();
        return _cliente;
    }
}
