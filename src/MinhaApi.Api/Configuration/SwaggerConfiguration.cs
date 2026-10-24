using System.Reflection;
using Microsoft.OpenApi;

namespace MinhaApi.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfigurado(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MinhaApi",
                Version = "v1"
            });

            // Botao "Authorize" do Swagger: cole o token devolvido por /api/v1/auth/login.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT obtido em POST /api/v1/auth/login."
            });
            options.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
            });

            // Le o arquivo XML gerado pelo GenerateDocumentationFile (csproj) e
            // usa os "/// <summary>" das controllers como descricao no Swagger.
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
