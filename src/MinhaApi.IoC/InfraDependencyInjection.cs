using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Infra.Config;
using MinhaApi.Infra.Produtos.Repositories;

namespace MinhaApi.IoC;

public static class InfraDependencyInjection
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("MySql");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:MySql nao configurado.");
        }

        // Unica fonte das configuracoes do MySQL; a API nao sobe se forem invalidas.
        services
            .AddOptions<MySqlOptions>()
            .Bind(config.GetSection(MySqlOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // DbContext: Scoped, UM por requisicao HTTP. NUNCA Singleton (README §7.1).
        services.AddDbContext<MinhaApiDbContext>((provider, options) =>
        {
            var mySql = provider.GetRequiredService<IOptions<MySqlOptions>>().Value;

            options.UseMySql(
                connectionString,
                new MySqlServerVersion(Version.Parse(mySql.ServerVersion)),
                opcoes => opcoes.CommandTimeout(mySql.CommandTimeout));
        });

        // UnitOfWork tambem Scoped - precisa usar o MESMO DbContext da requisicao,
        // senao a transacao seria aberta num contexto diferente do que os
        // repositorios estao usando.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IProdutoRepository, ProdutoRepository>();

        return services;
    }
}
