namespace MinhaApi.Api.Configuration;

public static class CorsConfiguration
{
    public const string PoliticaPadrao = "Padrao";

    public static IServiceCollection AddCorsConfigurada(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var origensPermitidas = configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];

        // Fora de Development as origens vem da configuracao do ambiente (nunca
        // AllowAnyOrigin). Sem elas a API nao sobe: erro claro no deploy em vez de CORS
        // bloqueando o front em silencio. Ex: variavel Cors__OrigensPermitidas__0.
        if (origensPermitidas.Length == 0 && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Cors:OrigensPermitidas nao configurado. Informe as origens permitidas do ambiente (ex: variavel Cors__OrigensPermitidas__0).");
        }

        services.AddCors(options =>
            options.AddPolicy(PoliticaPadrao, policy => policy
                .WithOrigins(origensPermitidas)
                .AllowAnyHeader()
                .WithExposedHeaders("X-Correlation-Id") // o front precisa ler esse header
                .AllowAnyMethod()));

        return services;
    }
}
