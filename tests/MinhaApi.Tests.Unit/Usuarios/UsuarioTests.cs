using AwesomeAssertions;
using MinhaApi.CrossCutting.Enums;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Usuarios.Entities;
using Xunit;

namespace MinhaApi.Tests.Unit.Usuarios;

public class UsuarioTests
{
    [Fact]
    public void ConstrutorValidoDeveNormalizarEmailEAtivar()
    {
        var usuario = new Usuario("Maria", "  Maria@Teste.COM ", "hash");

        usuario.Email.Should().Be("maria@teste.com");
        usuario.Situacao.Should().Be(Situacao.Ativo);
        usuario.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "a@b.com", "hash")]
    [InlineData("Maria", "sem-arroba", "hash")]
    [InlineData("Maria", "a@b.com", "")]
    public void ConstrutorInvalidoDeveLancarRegraDeNegocio(string nome, string email, string hash)
    {
        var acao = () => new Usuario(nome, email, hash);

        acao.Should().Throw<RegraDeNegocioException>();
    }
}
