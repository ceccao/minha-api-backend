using Mapster;
using Microsoft.Extensions.DependencyInjection;
using MinhaApi.Application.Autenticacao.Services;
using MinhaApi.Application.Autenticacao.Services.Interfaces;

namespace MinhaApi.IoC;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        services.AddScoped<AdminSeedService>();

        // Mapster estrito: todo par origem/destino precisa de um IRegister (Profile).
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
        TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IAutenticacaoService).Assembly);

        return services;
    }
}
