using MinhaApi.CrossCutting.Handlers;
using MinhaApi.CrossCutting.Middlewares;
using Serilog;

namespace MinhaApi.Api.Configuration;

public static class ApiConfiguration
{
    private const long LimiteCorpoRequisicaoBytes = 10 * 1024 * 1024; // 10 MB

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Mantém o sufixo Async no nome das actions (nameof(...) em CreatedAtAction depende disso).
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddSwaggerConfigurado();
        services.AddCorsConfigurada(configuration, environment);
        services.AddAutenticacao();
        services.AddRateLimitConfigurado();

        services.AddHealthChecks()
            .AddMySql(configuration.GetConnectionString("MySql") ?? string.Empty, name: "mysql");

        return services;
    }

    public static WebApplicationBuilder ConfigurarHost(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(CrossCutting.Logging.SerilogConfiguration.Configurar);

        // Em desenvolvimento, falha se um Singleton capturar algo Scoped (como o DbContext).
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
        // O Swagger vem antes da autenticação: a política "todo endpoint exige login" barraria a tela dele.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapTokenDesenvolvimento();                // so existe em Development

        app.Lifetime.ApplicationStopping.Register(() =>
            Log.Information("Aplicacao esta parando - drenando requisicoes em andamento..."));

        return app;
    }
}
