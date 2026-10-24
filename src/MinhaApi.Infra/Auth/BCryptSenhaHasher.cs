using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Infra.Auth;

public class BCryptSenhaHasher : ISenhaHasher
{
    // Custo do BCrypt: cada +1 dobra o tempo. 12 e um bom padrao hoje (~250 ms).
    private const int FatorDeTrabalho = 12;

    // Hash de uma senha qualquer, gerado uma vez: usado para gastar o mesmo tempo
    // quando o usuario nao existe (anti-enumeracao por tempo de resposta).
    private static readonly string HashFalso = BCrypt.Net.BCrypt.HashPassword("senha-falsa-para-igualar-tempo", FatorDeTrabalho);

    public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, FatorDeTrabalho);

    public bool Verificar(string senha, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.Verify(senha, HashFalso);
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
