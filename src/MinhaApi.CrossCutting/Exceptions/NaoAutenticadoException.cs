using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

// A mensagem é genérica de propósito: não revela se o usuário existe.
public class NaoAutenticadoException() : AppException("Credenciais inválidas.")
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
