using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Usuarios.Repositories;
using MinhaApi.Infra.Auth;
using MinhaApi.Infra.Config;
using MinhaApi.Infra.Usuarios.Repositories;

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

        services
            .AddOptions<MySqlOptions>()
            .Bind(config.GetSection(MySqlOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Scoped: um DbContext por requisição.
        services.AddDbContext<MinhaApiDbContext>((provider, options) =>
        {
            var mySql = provider.GetRequiredService<IOptions<MySqlOptions>>().Value;

            options.UseMySql(
                connectionString,
                new MySqlServerVersion(Version.Parse(mySql.ServerVersion)),
                opcoes => opcoes.CommandTimeout(mySql.CommandTimeout));
        });

        // Scoped também: precisa do mesmo DbContext da requisição.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddSingleton<ISenhaHasher, BCryptSenhaHasher>();
        services.AddSingleton<IGeradorToken, JwtGeradorToken>();

        return services;
    }
}
