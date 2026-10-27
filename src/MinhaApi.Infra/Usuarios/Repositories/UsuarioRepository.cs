using MinhaApi.Domain.Usuarios.Entities;
using MinhaApi.Domain.Usuarios.Repositories;
using MinhaApi.Infra.Config;

namespace MinhaApi.Infra.Usuarios.Repositories;

public class UsuarioRepository(MinhaApiDbContext context) : RepositorioBase<Usuario>(context), IUsuarioRepository
{
}
