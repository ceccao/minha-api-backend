using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Api.Configuration;

// Token de DESENVOLVIMENTO para usar no Swagger (botao "Authorize") sem passar pelo login.
// O endpoint so e registrado quando o ambiente e Development: em Staging/Production a rota
// nao existe (404), entao nao ha como gerar token sem credenciais fora da maquina do dev.
// O usuario do token e apenas um objeto em memoria: nada e lido nem gravado no banco.
public static class TokenDesenvolvimentoConfiguration
{
    public const string Rota = "/api/v1/auth/token-desenvolvimento";

    public static WebApplication MapTokenDesenvolvimento(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapPost(Rota, (IGeradorToken geradorToken) =>
            {
                var usuarioDeDesenvolvimento = new Usuario("Desenvolvimento", "dev@localhost", "sem-senha");
                TokenGerado token = geradorToken.Gerar(usuarioDeDesenvolvimento);

                return Results.Ok(new LoginResponse { Token = token.Token, ExpiraEm = token.ExpiraEm });
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Gera um token JWT de desenvolvimento (somente em Development).")
            .WithDescription("Copie o valor de 'token' e cole no botao Authorize do Swagger. Nao existe fora de Development.")
            .Produces<LoginResponse>(StatusCodes.Status200OK);

        return app;
    }
}
