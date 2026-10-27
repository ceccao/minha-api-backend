using AwesomeAssertions;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Domain.Produtos.Services;
using NSubstitute;
using Xunit;

namespace MinhaApi.Tests.Unit.Produtos.Services;

public class ProdutoDomainServiceTests
{
    private readonly IProdutoRepository _repositorio = Substitute.For<IProdutoRepository>();
    private readonly ProdutoDomainService _sut;

    public ProdutoDomainServiceTests()
    {
        _sut = new ProdutoDomainService(_repositorio);
    }

    [Fact]
    public async Task ValidarAsyncQuandoNaoExisteDeveLancarNaoEncontrado()
    {
        _repositorio.RecuperarAsync(99, Arg.Any<CancellationToken>()).Returns((Produto?)null);

        var acao = async () => await _sut.ValidarAsync(99, CancellationToken.None);

        await acao.Should().ThrowAsync<NaoEncontradoException<Produto>>();
    }

    [Fact]
    public async Task EditarAsyncSemVersionDeveEditar()
    {
        var produto = new Produto("Mouse", 50m);
        _repositorio.RecuperarAsync(1, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _sut.EditarAsync(1, new ProdutoCommand("Mouse Gamer", 80m), CancellationToken.None);

        resultado.Nome.Should().Be("Mouse Gamer");
        await _repositorio.Received(1).EditarAsync(produto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarAsyncComVersionDesatualizadaDeveLancarConflitoSemEditar()
    {
        var produto = new Produto("Mouse", 50m); // Version 0
        _repositorio.RecuperarAsync(1, Arg.Any<CancellationToken>()).Returns(produto);

        var acao = async () => await _sut.EditarAsync(1, new ProdutoCommand("Mouse Gamer", 80m, Version: 3), CancellationToken.None);

        await acao.Should().ThrowAsync<ConflitoException>();
        await _repositorio.DidNotReceive().EditarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarAsyncComVersionAtualDeveEditar()
    {
        var produto = new Produto("Mouse", 50m); // Version 0
        _repositorio.RecuperarAsync(1, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _sut.EditarAsync(1, new ProdutoCommand("Mouse Gamer", 80m, Version: 0), CancellationToken.None);

        resultado.Preco.Should().Be(80m);
    }
}
