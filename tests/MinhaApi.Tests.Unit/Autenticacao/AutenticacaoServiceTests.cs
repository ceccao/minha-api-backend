using System.Linq.Expressions;
using AwesomeAssertions;
using MinhaApi.Application.Autenticacao;
using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Application.Autenticacao.Services;
using MinhaApi.CrossCutting.Enums;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;
using MinhaApi.Domain.Usuarios.Repositories;
using NSubstitute;
using Xunit;

namespace MinhaApi.Tests.Unit.Autenticacao;

public class AutenticacaoServiceTests
{
    private readonly IUsuarioRepository _repositorio = Substitute.For<IUsuarioRepository>();
    private readonly ISenhaHasher _hasher = Substitute.For<ISenhaHasher>();
    private readonly IGeradorToken _gerador = Substitute.For<IGeradorToken>();
    private readonly AutenticacaoService _sut;

    public AutenticacaoServiceTests()
    {
        _sut = new AutenticacaoService(_repositorio, _hasher, _gerador, new LoginRequestValidator());
    }

    private static Usuario NovoUsuario() => new("Maria", "maria@teste.com", "hash-da-senha");

    private void RepositorioRetorna(Usuario? usuario)
        => _repositorio.RecuperarAsync(Arg.Any<Expression<Func<Usuario, bool>>>(), Arg.Any<CancellationToken>()).Returns(usuario);

    [Fact]
    public async Task LoginComCredenciaisValidasDeveDevolverToken()
    {
        var usuario = NovoUsuario();
        RepositorioRetorna(usuario);
        _hasher.Verificar("senha123", "hash-da-senha").Returns(true);
        var expira = DateTime.UtcNow.AddHours(1);
        _gerador.Gerar(usuario).Returns(new TokenGerado("token-jwt", expira));

        var resultado = await _sut.LoginAsync(new LoginRequest { Email = "Maria@Teste.com", Senha = "senha123" }, CancellationToken.None);

        resultado.Token.Should().Be("token-jwt");
        resultado.ExpiraEm.Should().Be(expira);
    }

    [Fact]
    public async Task LoginComSenhaErradaEUsuarioInexistenteDevemFalharIgual()
    {
        // Anti-enumeracao: mesma exception e mesma mensagem nos dois casos.
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string?>()).Returns(false);

        RepositorioRetorna(NovoUsuario());
        var senhaErrada = await Record.ExceptionAsync(() => _sut.LoginAsync(new LoginRequest { Email = "maria@teste.com", Senha = "errada" }, CancellationToken.None));

        RepositorioRetorna(null);
        var inexistente = await Record.ExceptionAsync(() => _sut.LoginAsync(new LoginRequest { Email = "ninguem@teste.com", Senha = "qualquer" }, CancellationToken.None));

        senhaErrada.Should().BeOfType<NaoAutenticadoException>();
        inexistente.Should().BeOfType<NaoAutenticadoException>();
        inexistente!.Message.Should().Be(senhaErrada!.Message);
    }

    [Fact]
    public async Task LoginDeUsuarioInexistenteDeveVerificarSenhaComHashNulo()
    {
        RepositorioRetorna(null);

        await Record.ExceptionAsync(() => _sut.LoginAsync(new LoginRequest { Email = "ninguem@teste.com", Senha = "qualquer" }, CancellationToken.None));

        // Gasta o mesmo tempo de uma verificacao real (anti-enumeracao por tempo).
        _hasher.Received(1).Verificar("qualquer", null);
    }

    [Fact]
    public async Task LoginDeUsuarioInativoDeveFalhar()
    {
        var usuario = NovoUsuario();
        typeof(Usuario).GetProperty(nameof(Usuario.Situacao))!.SetValue(usuario, Situacao.Inativo);
        RepositorioRetorna(usuario);
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string?>()).Returns(true);

        var acao = async () => await _sut.LoginAsync(new LoginRequest { Email = "maria@teste.com", Senha = "senha123" }, CancellationToken.None);

        await acao.Should().ThrowAsync<NaoAutenticadoException>();
        _gerador.DidNotReceive().Gerar(Arg.Any<Usuario>());
    }

    [Theory]
    [InlineData("", "senha")]
    [InlineData("a@b.com", "")]
    public async Task LoginComCamposVaziosDeveLancarEntidadeInvalida(string email, string senha)
    {
        var acao = async () => await _sut.LoginAsync(new LoginRequest { Email = email, Senha = senha }, CancellationToken.None);

        await acao.Should().ThrowAsync<EntidadeInvalidaException>();
        await _repositorio.DidNotReceive().RecuperarAsync(Arg.Any<Expression<Func<Usuario, bool>>>(), Arg.Any<CancellationToken>());
    }
}
