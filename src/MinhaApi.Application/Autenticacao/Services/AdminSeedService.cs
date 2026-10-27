using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;
using MinhaApi.Domain.Usuarios.Repositories;

namespace MinhaApi.Application.Autenticacao.Services;

// Cria o primeiro usuario quando nao existe nenhum (a API nao tem cadastro aberto).
// So age se e-mail e senha forem informados (Seed:AdminEmail / Seed:AdminSenha, via User
// Secrets ou variavel de ambiente) e a tabela estiver vazia. Depois do primeiro acesso,
// remova a senha da configuracao.
public class AdminSeedService(IUsuarioRepository usuarioRepository, ISenhaHasher senhaHasher, IUnitOfWork unitOfWork)
{
    public async Task<bool> GarantirAdminAsync(string? email, string? senha, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        if (await usuarioRepository.RecuperarAsync(u => u.Id > 0, cancellationToken) is not null)
        {
            return false;
        }

        var admin = new Usuario("Administrador", email, senhaHasher.Gerar(senha));
        await usuarioRepository.InserirAsync(admin, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return true;
    }
}
