using FluentValidation;
using Identity.Application.Abstracoes;
using Identity.Application.Comum;
using Identity.Domain.Comum;
using Identity.Domain.Usuarios;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Doadores;

/// <summary>
/// Caso de uso do cadastro público de doador: valida, confere e-mail e CPF únicos, protege senha e CPF,
/// registra o consentimento e grava. O log leva só o identificador do doador.
/// </summary>
/// <param name="usuarios">Repositório de usuários.</param>
/// <param name="cifrador">Cifrador e hash do CPF.</param>
/// <param name="hasherSenha">Gerador de hash de senha.</param>
/// <param name="validador">Validador do pedido.</param>
/// <param name="relogio">Relógio da aplicação.</param>
/// <param name="opcoes">Opções do cadastro, com a versão vigente da política de privacidade.</param>
/// <param name="metricas">Métrica de doadores cadastrados.</param>
/// <param name="log">Logger; nunca recebe dado pessoal.</param>
public sealed partial class CadastrarDoador(
    IUsuarioRepositorio usuarios,
    ICifradorCpf cifrador,
    IHasherSenha hasherSenha,
    IValidator<CadastrarDoadorRequest> validador,
    TimeProvider relogio,
    CadastrarDoadorOpcoes opcoes,
    IMetricasDoadores metricas,
    ILogger<CadastrarDoador> log)
{
    /// <summary>
    /// Código do erro de e-mail já cadastrado.
    /// </summary>
    public const string CodigoEmailEmUso = "doador.email_em_uso";

    /// <summary>
    /// Código do erro de CPF já cadastrado.
    /// </summary>
    public const string CodigoCpfEmUso = "doador.cpf_em_uso";

    /// <summary>
    /// Executa o cadastro.
    /// </summary>
    /// <param name="request">Dados do cadastro.</param>
    /// <param name="cancelamento">Token de cancelamento.</param>
    /// <returns>O doador cadastrado, ou <c>validacao</c>, <c>doador.email_em_uso</c> ou <c>doador.cpf_em_uso</c>.</returns>
    public async Task<Result<CadastrarDoadorResponse>> ExecutarAsync(CadastrarDoadorRequest request, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validacao = await validador.ValidateAsync(request, cancelamento);
        if (!validacao.IsValid)
        {
            return Result.Falha<CadastrarDoadorResponse>(ErrosDeValidacao.Converter(validacao));
        }

        var agora = relogio.GetUtcNow();
        var email = Email.Criar(request.Email).Valor;
        var cpf = Cpf.Criar(request.Cpf).Valor;
        var consentimento = Consentimento.Criar(request.AceitePoliticaPrivacidade, opcoes.VersaoPolitica, agora);
        if (consentimento.EhFalha)
        {
            return Result.Falha<CadastrarDoadorResponse>(consentimento.Erro);
        }

        if (await usuarios.ExisteEmailAsync(email, cancelamento))
        {
            return Result.Falha<CadastrarDoadorResponse>(new Erro(CodigoEmailEmUso, "Este e-mail já está cadastrado."));
        }

        if (await usuarios.ExisteCpfAsync(cifrador.CalcularHash(cpf), cancelamento))
        {
            return Result.Falha<CadastrarDoadorResponse>(new Erro(CodigoCpfEmUso, "Este CPF já está cadastrado."));
        }

        var doador = Usuario.CadastrarDoador(request.NomeCompleto, email, cpf, hasherSenha.Hash(request.Senha!), consentimento.Valor, agora);
        if (doador.EhFalha)
        {
            return Result.Falha<CadastrarDoadorResponse>(doador.Erro);
        }

        try
        {
            await usuarios.AdicionarAsync(doador.Valor, cancelamento);
        }
        catch (UnicidadeVioladaException excecao)
        {
            return Result.Falha<CadastrarDoadorResponse>(excecao.Campo == CampoUnico.Email
                ? new Erro(CodigoEmailEmUso, "Este e-mail já está cadastrado.")
                : new Erro(CodigoCpfEmUso, "Este CPF já está cadastrado."));
        }

        metricas.RegistrarCadastro();
        LogCadastrado(log, doador.Valor.Id);

        var cadastrado = doador.Valor;
        return Result.Sucesso(new CadastrarDoadorResponse(cadastrado.Id.Valor, cadastrado.NomeCompleto, cadastrado.Email.Valor, cpf.Mascarado(), cadastrado.CriadoEm));
    }

    /// <summary>
    /// Registra o cadastro de um doador, só com o identificador.
    /// </summary>
    /// <param name="log">Logger do caso de uso.</param>
    /// <param name="usuarioId">Identificador do doador cadastrado.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Doador cadastrado: {UsuarioId}.")]
    private static partial void LogCadastrado(ILogger log, UsuarioId usuarioId);
}

/// <summary>
/// Opções do cadastro de doador.
/// </summary>
/// <param name="VersaoPolitica">Versão vigente da política de privacidade (<c>Lgpd__VersaoPolitica</c>).</param>
public sealed record CadastrarDoadorOpcoes(string VersaoPolitica);
