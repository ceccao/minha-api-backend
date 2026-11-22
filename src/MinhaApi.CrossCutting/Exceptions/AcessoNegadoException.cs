using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

public class AcessoNegadoException(string nome) : AppException($"Você não tem acesso a {nome}.")
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}
