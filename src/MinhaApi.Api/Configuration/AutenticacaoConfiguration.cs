using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MinhaApi.CrossCutting.Options;

namespace MinhaApi.Api.Configuration;

public static class AutenticacaoConfiguration
{
    public static IServiceCollection AddAutenticacao(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // As opcoes do JwtBearer leem o JwtOptions (validado na subida), assim a chave
        // existe em um lugar so e so e lida depois da configuracao estar completa.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // mantem "sub" e "email" como estao no token
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audiencia,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Chave))
                };
            });

        // Seguro por padrao: TODO endpoint exige usuario autenticado. Quem for publico
        // (login, health) precisa dizer isso explicitamente com [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
