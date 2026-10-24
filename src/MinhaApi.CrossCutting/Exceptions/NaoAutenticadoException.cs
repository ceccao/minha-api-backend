using Microsoft.AspNetCore.Http;

namespace MinhaApi.CrossCutting.Exceptions;

// Falha de autenticacao (401). A mensagem e sempre generica de proposito: nao revela se
// o usuario existe (anti-enumeracao de usuarios, README §6.3).
public class NaoAutenticadoException(string mensagem = "Credenciais inválidas.") : AppException(mensagem)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
