using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

public class ConflitoException(string nome)
    : AppException($"{nome} foi modificado ou removido por outra operação. Recarregue e tente novamente.")
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}
