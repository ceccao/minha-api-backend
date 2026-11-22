using AwesomeAssertions;
using MinhaApi.CrossCutting.Exceptions;

namespace MinhaApi.Tests.Unit.CrossCutting;

public class ExceptionsTests
{
    [Fact]
    public void NaoEncontradoDeveMontarMensagemComONome()
    {
        var excecao = new NaoEncontradoException("Cliente");

        excecao.Message.Should().Be("Cliente não foi encontrado.");
        excecao.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ConflitoDeveMontarMensagemComONome()
    {
        var excecao = new ConflitoException("Cliente");

        excecao.Message.Should().Be("Cliente foi modificado ou removido por outra operação. Recarregue e tente novamente.");
        excecao.StatusCode.Should().Be(409);
    }

    [Fact]
    public void AcessoNegadoDeveMontarMensagemComONome()
    {
        var excecao = new AcessoNegadoException("Pedido");

        excecao.Message.Should().Be("Você não tem acesso a Pedido.");
        excecao.StatusCode.Should().Be(403);
    }

    [Fact]
    public void EntidadeInvalidaDeveMontarMensagemComOCampo()
    {
        var excecao = new EntidadeInvalidaException("E-mail");

        excecao.Message.Should().Be("E-mail é inválido.");
        excecao.StatusCode.Should().Be(400);
    }

    [Fact]
    public void NaoAutenticadoDeveTerMensagemGenerica()
    {
        var excecao = new NaoAutenticadoException();

        excecao.Message.Should().Be("Credenciais inválidas.");
        excecao.StatusCode.Should().Be(401);
    }
}
