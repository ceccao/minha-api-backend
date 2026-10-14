
using MinhaApi.CrossCutting.Enums;

namespace MinhaApi.Domain.Produtos.Entities;

public class Produto
{
    public virtual int Id { get; protected set; }
    public virtual string Nome { get; protected set; }
    public virtual decimal Preco { get; protected set; }
    public virtual Situacao Situacao { get; protected set; }

    protected Produto() { }

    public Produto(string nome, decimal preco)
    {
        SetNome(nome);
        SetPreco(preco);

        Ativar();
    }

    public virtual void SetNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do produto é obrigatório.", nameof(nome));

        if (nome.Length is < 3 or > 100)
            throw new ArgumentException("Nome deve ter no mínimo 3 e máximo 100 caracteres.", nameof(nome));

        Nome = nome;
    }

    public virtual void SetPreco(decimal preco)
    {
        if (preco < 0)
            throw new ArgumentException("Preço não pode ser negativo.", nameof(preco));

        Preco = preco;
    }
    public virtual void Ativar()
    {
        Situacao = Situacao.Ativo;
    }

    public virtual void Inativar()
    {
        Situacao = Situacao.Inativo;
    }
}
