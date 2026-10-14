using AwesomeAssertions;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.Validators;
using Xunit;

namespace MinhaApi.Tests.Unit.Produtos.Validators;

public class ListarProdutosRequestValidatorTests
{
    private readonly ListarProdutosRequestValidator _sut = new();

    [Theory]
    [InlineData("Id")]
    [InlineData("nome")]
    [InlineData("Preco")]
    [InlineData("Situacao")]
    public void CampoPermitidoDeveSerValido(string campo)
    {
        var request = new ListarProdutosRequest { CpOrd = campo };

        _sut.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("SenhaSecreta")]
    [InlineData("Nome; DROP TABLE produto")]
    public void CampoForaDaListaDeveSerInvalido(string campo)
    {
        var request = new ListarProdutosRequest { CpOrd = campo };

        _sut.Validate(request).IsValid.Should().BeFalse();
    }
}
