using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

public class RegraDeNegocioException(string mensagem) : AppException(mensagem)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
