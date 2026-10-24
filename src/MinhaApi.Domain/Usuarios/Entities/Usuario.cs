using MinhaApi.CrossCutting.Enums;
using MinhaApi.CrossCutting.Exceptions;

namespace MinhaApi.Domain.Usuarios.Entities;

public class Usuario
{
    public const int NomeTamanhoMaximo = 100;
    public const int EmailTamanhoMaximo = 150;

    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public string SenhaHash { get; protected set; } = string.Empty;
    public Situacao Situacao { get; protected set; }

    // Exigido pelo EF Core para materializar a entidade ao ler do banco.
    protected Usuario() { }

    // Recebe o HASH da senha, nunca a senha: quem gera o hash e o ISenhaHasher.
    public Usuario(string nome, string email, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > NomeTamanhoMaximo)
        {
            throw new RegraDeNegocioException($"Nome é obrigatório e deve ter no máximo {NomeTamanhoMaximo} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > EmailTamanhoMaximo || !email.Contains('@'))
        {
            throw new RegraDeNegocioException("E-mail inválido.");
        }

        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new RegraDeNegocioException("Hash de senha é obrigatório.");
        }

        Nome = nome;
        Email = NormalizarEmail(email);
        SenhaHash = senhaHash;
        Situacao = Situacao.Ativo;
    }

    public bool Ativo => Situacao == Situacao.Ativo;

    // E-mail e sempre guardado e comparado em minusculas.
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
