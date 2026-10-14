using AwesomeAssertions;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.Services;
using MinhaApi.Application.Produtos.Validators;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Domain.Produtos.Repositories.Filters;
using MinhaApi.Domain.Produtos.Services.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MinhaApi.Tests.Unit.Produtos.Services;

public class ProdutoServiceTests
{
    private readonly IProdutoDomainService _produtoDomainService = Substitute.For<IProdutoDomainService>();
    private readonly IProdutoRepository _produtoRepository = Substitute.For<IProdutoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProdutoService _sut;

    public ProdutoServiceTests()
    {
        _sut = new ProdutoService(_produtoDomainService, _produtoRepository, _unitOfWork, new ProdutoRequestValidator(), new ListarProdutosRequestValidator());
    }

    [Fact]
    public async Task RecuperarAsyncDeveRetornarResponseQuandoValidarAsyncEncontraProduto()
    {
        var produto = new Produto("Mouse", 120m);
        _produtoDomainService.ValidarAsync(1, Arg.Any<CancellationToken>()).Returns(produto);

        var resultado = await _sut.RecuperarAsync(1, CancellationToken.None);

        resultado.Nome.Should().Be("Mouse");
    }

    [Fact]
    public async Task RecuperarAsyncDevePropagarNaoEncontradoExceptionDoValidarAsync()
    {
        _produtoDomainService.ValidarAsync(99, Arg.Any<CancellationToken>())
            .Throws(new NaoEncontradoException<Produto>(99));

        var acao = async () => await _sut.RecuperarAsync(99, CancellationToken.None);

        await acao.Should().ThrowAsync<NaoEncontradoException<Produto>>();
    }

    [Fact]
    public async Task ListarAsyncDeveRetornarPaginacaoConsultaDeResponses()
    {
        var request = new ListarProdutosRequest();
        var produtos = new List<Produto> { new("Produto A", 10m), new("Produto B", 20m) };

        _produtoRepository.Filtrar(Arg.Any<ProdutoListarFilter>()).Returns(produtos.AsQueryable());
        _produtoRepository
            .ListarAsync(Arg.Any<IQueryable<Produto>>(), request.Qt, request.Pg, request.CpOrd, request.TpOrd, Arg.Any<CancellationToken>())
            .Returns(new PaginacaoConsulta<Produto>(produtos, produtos.Count, request.Pg, request.Qt));

        var resultado = await _sut.ListarAsync(request, CancellationToken.None);

        resultado.TotalItens.Should().Be(2);
        resultado.Itens.Should().HaveCount(2);
    }

    [Fact]
    public async Task InserirAsyncDeveChamarDomainServiceERetornarResponse()
    {
        var request = new ProdutoRequest { Nome = "Teclado", Preco = 350m };
        var produtoCriado = new Produto("Teclado", 350m);
        _produtoDomainService.InserirAsync(Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>())
            .Returns(produtoCriado);

        var resultado = await _sut.InserirAsync(request, CancellationToken.None);

        resultado.Nome.Should().Be("Teclado");
        resultado.Preco.Should().Be(350m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InserirAsyncQuandoDomainServiceLancaExceptionDeveFazerRollbackERepropagar()
    {
        var request = new ProdutoRequest { Nome = "Teclado", Preco = 350m };
        _produtoDomainService.InserirAsync(Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>())
            .Throws(new RegraDeNegocioException("Nome deve ter no mínimo 3 e máximo 100 caracteres."));

        var acao = async () => await _sut.InserirAsync(request, CancellationToken.None);

        await acao.Should().ThrowAsync<RegraDeNegocioException>();
        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarAsyncDeveChamarDomainServiceERetornarResponseAtualizado()
    {
        var request = new ProdutoRequest { Nome = "Nome Novo", Preco = 200m };
        var produtoEditado = new Produto("Nome Novo", 200m);
        _produtoDomainService.EditarAsync(1, Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>())
            .Returns(produtoEditado);

        var resultado = await _sut.EditarAsync(1, request, CancellationToken.None);

        resultado.Nome.Should().Be("Nome Novo");
        resultado.Preco.Should().Be(200m);
    }

    [Fact]
    public async Task EditarAsyncDevePropagarNaoEncontradoExceptionDoDomainService()
    {
        var request = new ProdutoRequest { Nome = "Nome", Preco = 100m };
        _produtoDomainService.EditarAsync(99, Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>())
            .Throws(new NaoEncontradoException<Produto>(99));

        var acao = async () => await _sut.EditarAsync(99, request, CancellationToken.None);

        await acao.Should().ThrowAsync<NaoEncontradoException<Produto>>();
    }

    [Fact]
    public async Task ExcluirAsyncDeveChamarInativarAsyncNaoAtivarAsync()
    {
        // Este teste existe especificamente pra travar o bug que encontramos:
        // ExcluirAsync chamava AtivarAsync por engano (copy-paste).
        var produto = new Produto("Produto", 100m);
        _produtoDomainService.InativarAsync(1, Arg.Any<CancellationToken>()).Returns(produto);

        await _sut.ExcluirAsync(1, CancellationToken.None);

        await _produtoDomainService.Received(1).InativarAsync(1, Arg.Any<CancellationToken>());
        await _produtoDomainService.DidNotReceive().AtivarAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExcluirAsyncDevePropagarNaoEncontradoExceptionDoDomainService()
    {
        _produtoDomainService.InativarAsync(99, Arg.Any<CancellationToken>())
            .Throws(new NaoEncontradoException<Produto>(99));

        var acao = async () => await _sut.ExcluirAsync(99, CancellationToken.None);

        await acao.Should().ThrowAsync<NaoEncontradoException<Produto>>();
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("ab", 10)]
    [InlineData("Produto valido", -1)]
    public async Task InserirAsyncComRequestInvalidoDeveLancarEntidadeInvalidaSemTocarNoDomain(string nome, decimal preco)
    {
        var request = new ProdutoRequest { Nome = nome, Preco = preco };

        var acao = async () => await _sut.InserirAsync(request, CancellationToken.None);

        await acao.Should().ThrowAsync<EntidadeInvalidaException>();
        await _produtoDomainService.DidNotReceive().InserirAsync(Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarAsyncComRequestInvalidoDeveLancarEntidadeInvalidaSemTocarNoDomain()
    {
        var request = new ProdutoRequest { Nome = "", Preco = 10m };

        var acao = async () => await _sut.EditarAsync(1, request, CancellationToken.None);

        await acao.Should().ThrowAsync<EntidadeInvalidaException>();
        await _produtoDomainService.DidNotReceive().EditarAsync(Arg.Any<int>(), Arg.Any<ProdutoCommand>(), Arg.Any<CancellationToken>());
    }
}
