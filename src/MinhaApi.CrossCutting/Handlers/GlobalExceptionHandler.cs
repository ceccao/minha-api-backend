using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinhaApi.CrossCutting.Exceptions;

namespace MinhaApi.CrossCutting.Handlers;

// Ponto unico de traducao exception -> HTTP -> log (IExceptionHandler, o padrao atual
// do ASP.NET Core). Registrado com AddExceptionHandler e ativado por UseExceptionHandler.
// Resposta sempre em ProblemDetails (RFC 7807) com traceId.
public partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception is AppException appException
            ? appException.StatusCode
            : StatusCodes.Status500InternalServerError;

        // AppException = erro de negocio esperado (warning, nao polui alerta).
        // Qualquer outra coisa = erro tecnico inesperado (error, aciona alerta real).
        if (exception is AppException)
        {
            LogErroDeNegocio(logger, exception.Message, exception);
        }
        else
        {
            LogErroNaoTratado(logger, exception);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = MensagemPublica(exception),
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is EntidadeInvalidaException entidadeInvalida)
        {
            problemDetails.Extensions["errors"] = entidadeInvalida.Erros;
        }

        // Em producao, nunca expor stack trace, nome de tabela ou detalhe interno (README §6.3).
        if (environment.IsDevelopment() && exception is not AppException)
        {
            problemDetails.Detail = exception.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }

    // AppException.Message e pensada para ser publica. Qualquer outro erro so mostra a
    // mensagem real em Development; fora dele a resposta e generica.
    private string MensagemPublica(Exception exception)
        => exception is AppException || environment.IsDevelopment()
            ? exception.Message
            : "Ocorreu um erro interno. Informe o traceId ao suporte.";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Erro de negocio tratado: {Message}")]
    private static partial void LogErroDeNegocio(ILogger logger, string message, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro nao tratado")]
    private static partial void LogErroNaoTratado(ILogger logger, Exception exception);
}
