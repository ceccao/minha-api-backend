using MinhaApi.CrossCutting.Enums;
using MinhaApi.CrossCutting.Exceptions;

namespace MinhaApi.Domain.Produtos.Entities;

public class Produto
{
    // Limites publicos para o validator da Application usar os mesmos valores.
    public const int NomeTamanhoMinimo = 3;
    public const int NomeTamanhoMaximo = 100;

    public int Id { get; protected set; }
    public string Nome { get; protected set; } = string.Empty;
    public decimal Preco { get; protected set; }
    public Situacao Situacao { get; protected set; }

    // Exigido pelo EF Core para materializar a entidade ao ler do banco.
    protected Produto() { }

    public Produto(string nome, decimal preco)
    {
        SetNome(nome);
        SetPreco(preco);
        Ativar();
    }

    public void SetNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraDeNegocioException("Nome do produto é obrigatório.");
        }

        if (nome.Length is < NomeTamanhoMinimo or > NomeTamanhoMaximo)
        {
            throw new RegraDeNegocioException($"Nome deve ter no mínimo {NomeTamanhoMinimo} e máximo {NomeTamanhoMaximo} caracteres.");
        }

        Nome = nome;
    }

    public void SetPreco(decimal preco)
    {
        if (preco < 0)
        {
            throw new RegraDeNegocioException("Preço não pode ser negativo.");
        }

        Preco = preco;
    }

    public void Ativar() => Situacao = Situacao.Ativo;

    public void Inativar() => Situacao = Situacao.Inativo;
}
