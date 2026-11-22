using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

public class EntidadeInvalidaException(string campo) : AppException($"{campo} é inválido.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
