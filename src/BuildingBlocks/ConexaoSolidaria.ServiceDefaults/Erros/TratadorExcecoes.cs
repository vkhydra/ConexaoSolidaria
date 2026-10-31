using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConexaoSolidaria.ServiceDefaults.Erros;

/// <summary>
/// Converte qualquer exceção não tratada em uma resposta <c>ProblemDetails</c> sem vazar detalhes internos.
/// Requisição mal formada vira 400; o resto vira 500 com o código <c>erro.inesperado</c>.
/// </summary>
public sealed class TratadorExcecoes : IExceptionHandler
{
    /// <summary>
    /// Serviço que escreve a resposta de erro no formato negociado com o cliente.
    /// </summary>
    private readonly IProblemDetailsService _servicoProblemas;

    /// <summary>
    /// Registro de log do tratador.
    /// </summary>
    private readonly ILogger<TratadorExcecoes> _logger;

    /// <summary>
    /// Ambiente de execução; só em desenvolvimento a mensagem da exceção aparece na resposta.
    /// </summary>
    private readonly IHostEnvironment _ambiente;

    /// <summary>
    /// Cria o tratador.
    /// </summary>
    /// <param name="servicoProblemas">Serviço que escreve respostas <c>ProblemDetails</c>.</param>
    /// <param name="logger">Registro de log.</param>
    /// <param name="ambiente">Ambiente de execução.</param>
    public TratadorExcecoes(IProblemDetailsService servicoProblemas, ILogger<TratadorExcecoes> logger, IHostEnvironment ambiente)
    {
        _servicoProblemas = servicoProblemas;
        _logger = logger;
        _ambiente = ambiente;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var requisicaoInvalida = exception is BadHttpRequestException;
        var status = requisicaoInvalida ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;

        if (requisicaoInvalida)
        {
            _logger.LogWarning("Requisição inválida em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        var problema = new ProblemDetails
        {
            Status = status,
            Title = CatalogoDeErros.TituloPara(status),
            Detail = _ambiente.IsDevelopment() ? exception.Message : null,
        };
        problema.Extensions["code"] = requisicaoInvalida ? CatalogoDeErros.Validacao : CatalogoDeErros.ErroInesperado;

        return await _servicoProblemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }
}
