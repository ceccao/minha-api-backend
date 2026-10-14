using AwesomeAssertions;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.Validators;
using Xunit;

namespace MinhaApi.Tests.Unit.Produtos.Validators;

public class ProdutoRequestValidatorTests
{
    private readonly ProdutoRequestValidator _sut = new();

    [Fact]
    public void RequestValidoNaoDeveGerarErros()
    {
        var resultado = _sut.Validate(new ProdutoRequest { Nome = "Teclado", Preco = 10m });

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void NomeVazioOuCurtoDeveSerInvalido(string nome)
    {
        var resultado = _sut.Validate(new ProdutoRequest { Nome = nome, Preco = 10m });

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NomeMaiorQueOLimiteDeveSerInvalido()
    {
        var resultado = _sut.Validate(new ProdutoRequest { Nome = new string('a', 101), Preco = 10m });

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PrecoNegativoDeveSerInvalido()
    {
        var resultado = _sut.Validate(new ProdutoRequest { Nome = "Teclado", Preco = -1m });

        resultado.IsValid.Should().BeFalse();
    }
}
