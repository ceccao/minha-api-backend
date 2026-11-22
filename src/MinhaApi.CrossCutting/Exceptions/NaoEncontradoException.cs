using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

public class NaoEncontradoException(string nome) : AppException($"{nome} não foi encontrado.")
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}
