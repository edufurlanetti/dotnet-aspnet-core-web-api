using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReservaLabs.Api.Services;

namespace ReservaLabs.Api.Infra;

/// <summary>
/// Tratador global (IExceptionHandler, .NET 8): converte as exceções de negócio
/// em respostas Problem Details (RFC 9457), sem try/catch nos controllers.
/// </summary>
public class TratadorDeExcecoes(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancelamento)
    {
        var (status, titulo) = excecao switch
        {
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            RegraNegocioException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (0, string.Empty)
        };

        if (status == 0)
            return false;   // exceção inesperada: deixa o ASP.NET Core responder 500

        contexto.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = excecao.Message }
        });
    }
}
