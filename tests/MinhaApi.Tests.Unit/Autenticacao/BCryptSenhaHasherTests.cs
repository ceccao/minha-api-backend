using AwesomeAssertions;
using MinhaApi.Infra.Auth;
using Xunit;

namespace MinhaApi.Tests.Unit.Autenticacao;

public class BCryptSenhaHasherTests
{
    private readonly BCryptSenhaHasher _sut = new();

    [Fact]
    public void HashNaoDeveConterASenhaEDeveSerVerificavel()
    {
        var hash = _sut.Gerar("MinhaSenha-123");

        hash.Should().NotContain("MinhaSenha");
        _sut.Verificar("MinhaSenha-123", hash).Should().BeTrue();
        _sut.Verificar("outra-senha", hash).Should().BeFalse();
    }

    [Fact]
    public void DoisHashesDaMesmaSenhaDevemSerDiferentes()
    {
        _sut.Gerar("MinhaSenha-123").Should().NotBe(_sut.Gerar("MinhaSenha-123"));
    }

    [Fact]
    public void HashNuloSempreDevolveFalso()
    {
        _sut.Verificar("qualquer", null).Should().BeFalse();
    }
}
