using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MinhaApi.Api.Configuration;

public static class RateLimitConfiguration
{
    public const string PoliticaLogin = "Login";

    private const int LimiteGeralPorMinuto = 100;
    private const int LimiteLoginPorMinuto = 5;

    public static IServiceCollection AddRateLimitConfigurado(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Limite geral por IP, aplicado a TODAS as rotas.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ChaveDoCliente(contexto), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LimiteGeralPorMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            // Mais rigido no login (anti forca bruta). Vale ALEM do limite geral.
            options.AddPolicy(PoliticaLogin, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(ChaveDoCliente(contexto), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LimiteLoginPorMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    // Atras de proxy/load balancer o IP real vem em X-Forwarded-For: configure
    // UseForwardedHeaders no deploy, senao todos os clientes compartilham o mesmo IP.
    private static string ChaveDoCliente(HttpContext contexto)
        => contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}
