using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinhaApi.IoC;

public static class CrossCuttingDependencyInjection
{
    // Reservado para registros futuros do CrossCutting via DI (ex: rate limiting
    // customizado, health checks especificos). Os middlewares (ExceptionMiddleware,
    // CorrelationIdMiddleware, SecurityHeadersMiddleware) NAO precisam de registro
    // aqui - o ASP.NET Core os ativa sozinho via UseMiddleware<T>() no Program.cs
    // (FASE 11), resolvendo as dependencias do construtor pela DI automaticamente.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Assinatura reservada para registros futuros que dependam de configuracao.")]
    public static IServiceCollection AddCrossCutting(this IServiceCollection services, IConfiguration config)
        => services;
}
