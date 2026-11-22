using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Entities;

namespace MinhaApi.Api.Configuration;

// Token de desenvolvimento para o botão "Authorize" do Swagger sem passar pelo login.
// Só é registrado em Development (nos demais ambientes a rota não existe).
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
