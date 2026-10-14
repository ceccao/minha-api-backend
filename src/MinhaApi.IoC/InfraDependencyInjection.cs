using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Infra.Config;
using MinhaApi.Infra.Produtos.Repositories;

namespace MinhaApi.IoC;

public static class InfraDependencyInjection
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql nao configurado.");

        var commandTimeout = config.GetValue("MySql:CommandTimeout", 30);

        // Versao do servidor explicita (nao AutoDetect): o AutoDetect abre conexao no
        // momento da configuracao e quebraria a subida da API com o banco fora do ar.
        // Ajuste MySql:ServerVersion conforme o ambiente (instalacao nativa, Docker, Azure).
        var serverVersion = new MySqlServerVersion(Version.Parse(config.GetValue("MySql:ServerVersion", "8.0.0")));

        // DbContext: Scoped, UM por requisicao HTTP. NUNCA Singleton (README §7.1).
        services.AddDbContext<MinhaApiDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySql => mySql.CommandTimeout(commandTimeout)));

        // UnitOfWork tambem Scoped - precisa usar o MESMO DbContext da requisicao,
        // senao a transacao seria aberta num contexto diferente do que os
        // repositorios estao usando.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IProdutoRepository, ProdutoRepository>();

        return services;
    }
}
