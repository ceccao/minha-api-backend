using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using MinhaApi.Application.Produtos.Profiles;
using MinhaApi.Application.Produtos.Services;
using MinhaApi.Application.Produtos.Services.Interfaces;
using MinhaApi.Application.Produtos.Validators;
using MinhaApi.Domain.Produtos.Services;
using MinhaApi.Domain.Produtos.Services.Interfaces;

namespace MinhaApi.IoC;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Domain Service - a logica de negocio (Criar/Editar/Excluir) mora aqui.
        // A Service da Application vira so um wrapper fino de transacao/validacao.
        services.AddScoped<IProdutoDomainService, ProdutoDomainService>();

        services.AddScoped<IProdutoService, ProdutoService>();

        // Validators da Application (ProdutoRequestValidator, ListarProdutosRequestValidator) -
        // evita o Domain precisar referenciar o FluentValidation.
        services.AddValidatorsFromAssemblyContaining<ProdutoRequestValidator>();

        // Mapster estrito: mapeamento nao registrado falha na hora (veja ProdutoProfile).
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
        TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;
        TypeAdapterConfig.GlobalSettings.Scan(typeof(ProdutoProfile).Assembly);

        return services;
    }
}
