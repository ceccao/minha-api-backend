using MinhaApi.CrossCutting.Handlers;
using MinhaApi.CrossCutting.Middlewares;
using Serilog;

namespace MinhaApi.Api.Configuration;

// Configuracao do que e HTTP/ASP.NET (a injecao de dependencia das camadas de negocio
// fica no IoC). Dois passos, na ordem em que o Program.cs os chama:
//   AddApi  -> registra servicos (antes do Build)
//   UseApi  -> monta o pipeline de middlewares (depois do Build); A ORDEM IMPORTA.
public static class ApiConfiguration
{
    private const long LimiteCorpoRequisicaoBytes = 10 * 1024 * 1024; // 10 MB

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Validacao e feita explicitamente na Application (ProdutoService), nao por filtro
        // automatico do MVC. SuppressAsyncSuffixInActionNames = false mantem o nome da
        // action igual ao do metodo (com "Async"), senao nameof(RecuperarAsync) no
        // CreatedAtAction nao bate com o nome real da rota.
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddSwaggerConfigurado();
        services.AddCorsConfigurada(configuration, environment);
        services.AddRateLimitConfigurado();

        // Health check real: so responde Healthy se conseguir falar com o banco.
        services.AddHealthChecks()
            .AddMySql(configuration.GetConnectionString("MySql") ?? string.Empty, name: "mysql");

        return services;
    }

    public static WebApplicationBuilder ConfigurarHost(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(CrossCutting.Logging.SerilogConfiguration.Configurar);

        // Captive Dependency (README §7.6): em desenvolvimento, falha se um Singleton
        // capturar algo Scoped (como o DbContext).
        builder.Host.UseDefaultServiceProvider((contexto, opcoes) =>
        {
            opcoes.ValidateScopes = contexto.HostingEnvironment.IsDevelopment();
            opcoes.ValidateOnBuild = contexto.HostingEnvironment.IsDevelopment();
        });

        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = LimiteCorpoRequisicaoBytes);

        return builder;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsConfiguration.PoliticaPadrao);
        app.UseRateLimiter();

        // UseAuthentication + UseAuthorization entram aqui na FASE 17 (JWT).

        app.MapControllers().RequireRateLimiting(RateLimitConfiguration.PoliticaPadrao);
        app.MapHealthChecks("/health");

        // Swagger UI somente em desenvolvimento.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Lifetime.ApplicationStopping.Register(() =>
            Log.Information("Aplicacao esta parando - drenando requisicoes em andamento..."));

        return app;
    }
}
