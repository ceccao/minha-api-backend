using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MinhaApi.CrossCutting.Options;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Infra.Auth;

public class JwtGeradorToken(IOptions<JwtOptions> opcoes) : IGeradorToken
{
    private readonly JwtOptions _opcoes = opcoes.Value;

    public TokenGerado Gerar(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_opcoes.ExpiracaoMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Audiencia,
            Expires = expiraEm,
            // Claims minimas: quem e o usuario. Nada de dado pessoal alem do necessario.
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Chave)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);
        return new TokenGerado(token, expiraEm);
    }
}
