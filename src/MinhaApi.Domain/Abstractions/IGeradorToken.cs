using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Domain.Abstractions;

public record TokenGerado(string Token, DateTime ExpiraEm);

public interface IGeradorToken
{
    TokenGerado Gerar(Usuario usuario);
}
