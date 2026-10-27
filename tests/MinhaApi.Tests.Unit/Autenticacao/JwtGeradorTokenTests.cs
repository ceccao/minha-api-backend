using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MinhaApi.CrossCutting.Options;
using MinhaApi.Domain.Usuarios.Entities;
using MinhaApi.Infra.Auth;
using Xunit;

namespace MinhaApi.Tests.Unit.Autenticacao;

public class JwtGeradorTokenTests
{
    private const string Chave = "chave-de-teste-com-mais-de-32-caracteres-ok";

    private static JwtOptions Opcoes() => new() { Chave = Chave, Emissor = "Emissor", Audiencia = "Audiencia", ExpiracaoMinutos = 30 };

    private static TokenValidationParameters Parametros(string chave = Chave) => new()
    {
        ValidIssuer = "Emissor",
        ValidAudience = "Audiencia",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave))
    };

    [Fact]
    public async Task TokenGeradoDeveSerValidoETerAsClaimsEsperadas()
    {
        var usuario = new Usuario("Maria", "maria@teste.com", "hash");
        var sut = new JwtGeradorToken(Options.Create(Opcoes()));

        var gerado = sut.Gerar(usuario);
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(gerado.Token, Parametros());

        resultado.IsValid.Should().BeTrue();
        resultado.Claims[JwtRegisteredClaimNames.Email].Should().Be("maria@teste.com");
        gerado.ExpiraEm.Should().BeAfter(DateTime.UtcNow.AddMinutes(29));
    }

    [Fact]
    public async Task TokenAssinadoComOutraChaveDeveSerRejeitado()
    {
        var usuario = new Usuario("Maria", "maria@teste.com", "hash");
        var gerado = new JwtGeradorToken(Options.Create(Opcoes())).Gerar(usuario);

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(gerado.Token, Parametros("outra-chave-totalmente-diferente-123456"));

        resultado.IsValid.Should().BeFalse();
    }
}
