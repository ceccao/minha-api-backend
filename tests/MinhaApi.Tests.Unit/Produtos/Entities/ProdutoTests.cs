using AwesomeAssertions;
using MinhaApi.CrossCutting.Enums;
using MinhaApi.Domain.Produtos.Entities;
using Xunit;

namespace MinhaApi.Tests.Unit.Produtos.Entities;

public class ProdutoTests
{
    [Fact]
    public void ConstructorComDadosValidosDeveCriarProdutoCorretamente()
    {
        var produto = new Produto("Teclado Mecânico", 350.00m);

        produto.Nome.Should().Be("Teclado Mecânico");
        produto.Preco.Should().Be(350.00m);
        produto.Situacao.Should().Be(Situacao.Ativo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorComNomeVazioOuEmBrancoDeveLancarArgumentException(string nomeInvalido)
    {
        var acao = () => new Produto(nomeInvalido, 100m);

        acao.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "nome");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("Produto com nome bem longo que ultrapassa exatamente o limite de cem caracteres permitidos no campo nome")]
    public void ConstructorComNomeForaDoTamanhoPermitidoDeveLancarArgumentException(string nomeInvalido)
    {
        var acao = () => new Produto(nomeInvalido, 100m);

        acao.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "nome");
    }

    [Fact]
    public void ConstructorComPrecoNegativoDeveLancarArgumentException()
    {
        var acao = () => new Produto("Produto Válido", -10m);

        acao.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "preco");
    }

    [Fact]
    public void SetNomeComNomeValidoDeveDefinirNome()
    {
        var produto = new Produto("Nome Original", 100m);

        produto.SetNome("Nome Novo");

        produto.Nome.Should().Be("Nome Novo");
    }

    [Fact]
    public void SetNomeComNomeVazioDeveLancarArgumentException()
    {
        var produto = new Produto("Nome Original", 100m);

        var acao = () => produto.SetNome("");

        acao.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "nome");
    }

    [Fact]
    public void SetPrecoComPrecoValidoDeveDefinirPreco()
    {
        var produto = new Produto("Produto", 100m);

        produto.SetPreco(200m);

        produto.Preco.Should().Be(200m);
    }

    [Fact]
    public void SetPrecoComPrecoNegativoDeveLancarArgumentException()
    {
        var produto = new Produto("Produto", 100m);

        var acao = () => produto.SetPreco(-1m);

        acao.Should().Throw<ArgumentException>()
            .Where(ex => ex.ParamName == "preco");
    }

    [Fact]
    public void InativarDeveMarcarSituacaoComoInativo()
    {
        var produto = new Produto("Produto", 100m);

        produto.Inativar();

        produto.Situacao.Should().Be(Situacao.Inativo);
    }

    [Fact]
    public void AtivarDeveMarcarSituacaoComoAtivo()
    {
        var produto = new Produto("Produto", 100m);
        produto.Inativar();

        produto.Ativar();

        produto.Situacao.Should().Be(Situacao.Ativo);
    }
}
