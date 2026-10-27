using MinhaApi.Application.Autenticacao.Services;

namespace MinhaApi.Api.Configuration;

public static class SeedConfiguration
{
    // Cria o primeiro usuario quando a tabela esta vazia e Seed:AdminEmail / Seed:AdminSenha
    // foram informados (User Secrets em dev; variavel de ambiente no primeiro deploy).
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        var email = app.Configuration["Seed:AdminEmail"];
        var senha = app.Configuration["Seed:AdminSenha"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            return;
        }

        using var escopo = app.Services.CreateScope();
        var seed = escopo.ServiceProvider.GetRequiredService<AdminSeedService>();

        if (await seed.GarantirAdminAsync(email, senha, CancellationToken.None))
        {
            Serilog.Log.Warning("Usuario administrador criado. Remova Seed:AdminSenha da configuracao.");
        }
    }
}
