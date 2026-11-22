using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinhaApi.CrossCutting.Options;

namespace MinhaApi.IoC;

public static class CrossCuttingDependencyInjection
{
    // Os middlewares e o GlobalExceptionHandler sao ativados na Api (UseMiddleware /
    // AddExceptionHandler). Aqui ficam as opcoes compartilhadas entre camadas.
    public static IServiceCollection AddCrossCutting(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
