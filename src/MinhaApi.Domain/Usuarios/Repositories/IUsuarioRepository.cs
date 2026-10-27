using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Domain.Usuarios.Repositories;

// Busca por e-mail: RecuperarAsync(u => u.Email == email, ct) da base.
public interface IUsuarioRepository : IRepositorioBase<Usuario>
{
}
